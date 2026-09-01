using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ChessArticle
{
    /// <summary>
    /// Calls the Anthropic Claude Messages API (api.anthropic.com/v1/messages)
    /// with no SDK. Configure ClaudeApiKey + ClaudeModel in
    /// App_Data/AppSettings/Application_ChessArticle.xml.
    ///
    /// Returns an AiResult shaped exactly like GeminiClient.Generate so the
    /// dispatcher in AiClient.cs can swap providers without the caller
    /// caring. Token counts come back in the response's usage block - we
    /// thread them through AiResult.InputTokens / OutputTokens so the AI
    /// usage log can compute dollar cost.
    ///
    /// JSON shape: we ask Claude to return a JSON object matching the
    /// 7-key article schema by stuffing the schema description into the
    /// system prompt (PromptBuilder.SystemPrompt already does this for
    /// Gemini). Anthropic doesn't have a native "responseSchema" param like
    /// Gemini - we rely on the prompt + post-parse retry logic upstream.
    /// </summary>
    public static class ClaudeClient
    {
        public static AiResult Generate(string systemPrompt, string userPrompt,
            AiOptions options = null)
        {
            var result = new AiResult();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            string apiKey = Secrets.Get(CA.AppSettings.ClaudeApiKey);
            string model  = Secrets.Get(CA.AppSettings.ClaudeModel, "claude-sonnet-4-5");

            double temperature = (options != null && options.Temperature.HasValue)
                ? options.Temperature.Value : 0.85;
            // Claude requires max_tokens. Article roundup ~8K output is safe
            // upper bound; we still respect a caller override.
            int maxTokens = (options != null && options.MaxTokens.HasValue
                             && options.MaxTokens.Value > 0)
                ? options.MaxTokens.Value : 8192;

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("PASTE-"))
            {
                result.Error = "Claude API key is not set. Put it in "
                    + "App_Data/AppSettings/Application_ChessArticle.xml under <Name>ClaudeApiKey</Name>.";
                return result;
            }

            var ser = new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 };
            var requestBody = new Dictionary<string, object>
            {
                { "model", model },
                { "max_tokens", maxTokens },
                { "temperature", temperature },
                // Claude's API takes the system prompt at the top level, NOT
                // in the messages list - that's the main shape diff vs Gemini.
                { "system", systemPrompt },
                { "messages", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "role", "user" },
                            { "content", userPrompt }
                        }
                    }
                }
            };
            string json = ser.Serialize(requestBody);

            const string url = "https://api.anthropic.com/v1/messages";
            byte[] payload = Encoding.UTF8.GetBytes(json);
            const int maxAttempts = 5;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.Timeout = 300000; // 5 minutes
                    req.Headers["x-api-key"] = apiKey;
                    req.Headers["anthropic-version"] = "2023-06-01";
                    req.ContentLength = payload.Length;
                    using (Stream s = req.GetRequestStream())
                    {
                        s.Write(payload, 0, payload.Length);
                    }

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        result.Text = ExtractText(ser, reader.ReadToEnd(), result);
                        result.Ok = result.Error.Length == 0 && result.Text.Length > 0;
                    }
                    sw.Stop();
                    result.DurationMs = sw.ElapsedMilliseconds;
                    break;
                }
                catch (WebException wex)
                {
                    int status = 0;
                    var httpResp = wex.Response as HttpWebResponse;
                    if (httpResp != null) status = (int)httpResp.StatusCode;

                    // 429 = rate limit, 529 = overloaded, 5xx = transient
                    bool retryable = status == 429 || status == 500
                        || status == 502 || status == 503 || status == 529;
                    if (retryable && attempt < maxAttempts)
                    {
                        System.Threading.Thread.Sleep(8000 * attempt);
                        continue;
                    }
                    result.Error = ReadWebError(wex);
                    sw.Stop();
                    result.DurationMs = sw.ElapsedMilliseconds;
                    break;
                }
                catch (Exception ex)
                {
                    result.Error = "Claude request failed: " + ex.Message;
                    sw.Stop();
                    result.DurationMs = sw.ElapsedMilliseconds;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Parse the Claude Messages API response. Shape (success):
        /// {
        ///   "id": "msg_...",
        ///   "type": "message",
        ///   "content": [ { "type": "text", "text": "..." }, ... ],
        ///   "usage": { "input_tokens": N, "output_tokens": N },
        ///   ...
        /// }
        /// Shape (error): { "type": "error", "error": { "type": "...", "message": "..." } }
        /// </summary>
        private static string ExtractText(JavaScriptSerializer ser, string respText, AiResult result)
        {
            var root = ser.DeserializeObject(respText) as Dictionary<string, object>;
            if (root == null)
            {
                result.Error = "Unexpected Claude response.";
                return "";
            }

            // Usage block first - we want token counts even if extraction below fails.
            object usageObj;
            if (root.TryGetValue("usage", out usageObj))
            {
                var usage = usageObj as Dictionary<string, object>;
                if (usage != null)
                {
                    result.InputTokens  = AsInt(usage, "input_tokens");
                    result.OutputTokens = AsInt(usage, "output_tokens");
                }
            }

            // Error envelope?
            object typeObj;
            if (root.TryGetValue("type", out typeObj)
                && string.Equals(Convert.ToString(typeObj), "error", StringComparison.OrdinalIgnoreCase))
            {
                object errObj;
                if (root.TryGetValue("error", out errObj))
                {
                    var e = errObj as Dictionary<string, object>;
                    object msg;
                    if (e != null && e.TryGetValue("message", out msg))
                    {
                        result.Error = "Claude API error: " + Convert.ToString(msg);
                        return "";
                    }
                }
                result.Error = "Claude API returned an error envelope.";
                return "";
            }

            // content[] is an array of blocks; we concatenate the text blocks.
            object contentObj;
            if (root.TryGetValue("content", out contentObj))
            {
                var arr = contentObj as object[];
                if (arr != null)
                {
                    var sb = new StringBuilder();
                    foreach (object block in arr)
                    {
                        var bd = block as Dictionary<string, object>;
                        if (bd == null) continue;
                        object bType;
                        if (!bd.TryGetValue("type", out bType)) continue;
                        if (!string.Equals(Convert.ToString(bType), "text",
                                StringComparison.OrdinalIgnoreCase)) continue;
                        object t;
                        if (bd.TryGetValue("text", out t))
                            sb.Append(Convert.ToString(t));
                    }
                    return sb.ToString();
                }
            }

            result.Error = "No text content in the Claude response.";
            return "";
        }

        private static string ReadWebError(WebException wex)
        {
            string body = "";
            try
            {
                if (wex.Response != null)
                {
                    using (var reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        body = reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                // ignore - fall back to the exception message
            }
            return body.Length > 0
                ? "Claude API error: " + body
                : "Claude API request failed: " + wex.Message;
        }

        private static int AsInt(Dictionary<string, object> d, string k)
        {
            object v;
            if (d.TryGetValue(k, out v) && v != null)
            {
                try { return Convert.ToInt32(v); } catch { return 0; }
            }
            return 0;
        }
    }
}
