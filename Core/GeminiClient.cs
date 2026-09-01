using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ChessArticle
{
    /// <summary>
    /// Calls the Google Gemini API (generativelanguage.googleapis.com) with no
    /// SDK. Configure GeminiApiKey / GeminiModel in Web.config.
    /// </summary>
    public static class GeminiClient
    {
        public static AiResult Generate(string systemPrompt, string userPrompt,
            AiOptions options = null)
        {
            var result = new AiResult();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Secrets.Get chains env -> AppManager XML -> Web.config so prod
            // can pin the key in CHESSARTICLE_GEMINIAPIKEY without touching
            // disk. Model name is non-secret; lives in Application_ChessArticle.xml.
            string apiKey = Secrets.Get(CA.AppSettings.GeminiApiKey);
            string model = Secrets.Get(CA.AppSettings.GeminiModel, "gemini-2.5-flash");

            // Per-task overrides; defaults are the existing tuned values.
            double temperature = (options != null && options.Temperature.HasValue)
                ? options.Temperature.Value : 0.85;

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("PASTE-"))
            {
                result.Error = "Gemini API key is not set. Put it in Web.config under appSettings/GeminiApiKey.";
                return result;
            }

            var ser = new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 };
            var requestBody = new Dictionary<string, object>
            {
                { "systemInstruction", new Dictionary<string, object>
                    { { "parts", new object[] { new Dictionary<string, object> { { "text", systemPrompt } } } } }
                },
                { "contents", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "role", "user" },
                            { "parts", new object[] { new Dictionary<string, object> { { "text", userPrompt } } } },
                        }
                    }
                },
                { "generationConfig", new Dictionary<string, object>
                    {
                        { "temperature", temperature },
                        { "responseMimeType", "application/json" },
                        { "responseSchema", ArticleSchema() },
                    }
                },
            };
            string json = ser.Serialize(requestBody);

            string url = "https://generativelanguage.googleapis.com/v1beta/models/"
                + model + ":generateContent?key=" + Uri.EscapeDataString(apiKey);

            string payload = json;
            const int maxAttempts = 5;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    HttpOutcome http = Http.PostJson(url, payload, timeoutMs: 300000);

                    if (http.IsSuccess)
                    {
                        result.Text = ExtractText(ser, http.Body, result);
                        result.Ok = result.Error.Length == 0 && result.Text.Length > 0;
                        sw.Stop();
                        result.DurationMs = sw.ElapsedMilliseconds;
                        break; // success - leave the retry loop
                    }

                    // Gemini is frequently briefly overloaded - back off and retry.
                    bool retryable = http.Status == 429 || http.Status == 500
                                  || http.Status == 502 || http.Status == 503
                                  || http.Status == 529;
                    if (retryable && attempt < maxAttempts)
                    {
                        System.Threading.Thread.Sleep(8000 * attempt);
                        continue;
                    }
                    result.Error = DescribeError(http);
                    break;
                }
                catch (Exception ex)
                {
                    result.Error = "Gemini request failed: " + ex.Message;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Response schema for structured JSON output. Forcing the shape (not just
        /// the application/json MIME type) guarantees the 7 article keys come back
        /// as a parseable object every time.
        /// </summary>
        private static Dictionary<string, object> ArticleSchema()
        {
            string[] keys =
            {
                "title", "body_markdown", "image_prompt_scene",
                "image_prompt_board", "social_caption", "meta_description", "tags"
            };
            var props = new Dictionary<string, object>();
            foreach (string k in keys)
                props[k] = new Dictionary<string, object> { { "type", "string" } };

            return new Dictionary<string, object>
            {
                { "type", "object" },
                { "properties", props },
                { "required", keys },
                { "propertyOrdering", keys },
            };
        }

        private static string ExtractText(JavaScriptSerializer ser, string respText, AiResult result)
        {
            var root = ser.DeserializeObject(respText) as Dictionary<string, object>;
            if (root == null)
            {
                result.Error = "Unexpected Gemini response.";
                return "";
            }

            // Usage metadata - optional, populated best-effort.
            object usageObj;
            if (root.TryGetValue("usageMetadata", out usageObj))
            {
                var usage = usageObj as Dictionary<string, object>;
                if (usage != null)
                {
                    result.InputTokens = AsInt(usage, "promptTokenCount");
                    result.OutputTokens = AsInt(usage, "candidatesTokenCount");
                }
            }

            object cands;
            if (root.TryGetValue("candidates", out cands))
            {
                var arr = cands as object[];
                if (arr != null && arr.Length > 0)
                {
                    var c0 = arr[0] as Dictionary<string, object>;
                    object contentObj;
                    if (c0 != null && c0.TryGetValue("content", out contentObj))
                    {
                        var content = contentObj as Dictionary<string, object>;
                        object partsObj;
                        if (content != null && content.TryGetValue("parts", out partsObj))
                        {
                            var parts = partsObj as object[];
                            var sb = new StringBuilder();
                            if (parts != null)
                            {
                                foreach (object p in parts)
                                {
                                    var pd = p as Dictionary<string, object>;
                                    object t;
                                    if (pd != null && pd.TryGetValue("text", out t))
                                        sb.Append(Convert.ToString(t));
                                }
                            }
                            return sb.ToString();
                        }
                    }
                }
            }

            object errObj;
            if (root.TryGetValue("error", out errObj))
            {
                var e = errObj as Dictionary<string, object>;
                object msg;
                if (e != null && e.TryGetValue("message", out msg))
                {
                    result.Error = "Gemini API error: " + Convert.ToString(msg);
                    return "";
                }
            }

            result.Error = "No text content in the Gemini response.";
            return "";
        }

        private static string DescribeError(HttpOutcome http)
        {
            if (!http.Sent)
                return "Gemini API unreachable: " + http.TransportError;
            return http.Body.Length > 0
                ? "Gemini API error (HTTP " + http.Status + "): " + http.Body
                : "Gemini API returned HTTP " + http.Status + " with no body";
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
