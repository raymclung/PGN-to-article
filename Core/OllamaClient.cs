using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace ChessArticle
{
    /// <summary>
    /// Calls a local Ollama server (http://localhost:11434 by default) via its
    /// /api/chat endpoint. The Ollama server must be running ("ollama serve");
    /// no API key is needed. Configure OllamaUrl / OllamaModel in Web.config.
    /// </summary>
    public static class OllamaClient
    {
        public static AiResult Generate(string systemPrompt, string userPrompt,
            string modelConfigKey = "OllamaModel", AiOptions options = null)
        {
            var result = new AiResult();

            string baseUrl = Secrets.Get(CA.AppSettings.OllamaUrl, "http://localhost:11434");
            baseUrl = baseUrl.TrimEnd('/');

            string model = Secrets.Get(modelConfigKey, "llama3.1");

            // Defaults are tuned for chess prose:
            // - temperature 0.5: less rambling, more on-spec; chess content
            //   doesn't need wild creativity, accuracy + flow matter more.
            // - num_predict 3500: hard cap on output (~2600 words ceiling) so
            //   a runaway model can't burn 10 minutes generating filler.
            // - num_ctx 16384: full context window. Engine data + system prompt
            //   for a single game runs ~7-10k tokens combined.
            // The optional AiOptions argument overrides any of these per-task
            // (e.g. a reviewer pass that wants a deterministic temperature).
            double temperature = (options != null && options.Temperature.HasValue)
                ? options.Temperature.Value : 0.5;
            int numPredict = (options != null && options.MaxTokens.HasValue)
                ? options.MaxTokens.Value : 3500;
            int numCtx = (options != null && options.NumCtx.HasValue)
                ? options.NumCtx.Value : 16384;

            var ser = new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 };
            // format=json: Ollama enforces valid JSON output (no control-
            // character leaks like an un-tuned plain call would produce).
            var requestBody = new Dictionary<string, object>
            {
                { "model", model },
                { "stream", false },
                { "format", "json" },
                { "options", new Dictionary<string, object>
                    {
                        { "temperature",  temperature },
                        { "num_predict",  numPredict },
                        { "num_ctx",      numCtx },
                    }
                },
                { "messages", new object[]
                    {
                        new Dictionary<string, object> { { "role", "system" }, { "content", systemPrompt } },
                        new Dictionary<string, object> { { "role", "user" }, { "content", userPrompt } },
                    }
                },
            };
            string payload = ser.Serialize(requestBody);

            // Single retry on transient 5xx (Ollama returns 500 briefly when it
            // swaps a model out of VRAM under memory pressure). One retry with
            // a 5-second pause catches ~80% of these without bothering the user.
            const int maxAttempts = 2;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    HttpOutcome http = Http.PostJson(
                        baseUrl + "/api/chat", payload,
                        timeoutMs: 600000);   // 10 minutes - local models can be slow

                    if (http.IsSuccess)
                    {
                        ParseSuccess(ser, http.Body, result);
                        return result;
                    }

                    int status = http.Status;
                    string body = http.Body;

                    // 404 with "model not found" -> stop, give an actionable error.
                    if (status == 404 || body.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Error = "Ollama model '" + model + "' is not pulled. Run:\n"
                            + "    ollama pull " + model + "\n"
                            + "(or check the " + modelConfigKey + " setting for the right name).";
                        return result;
                    }

                    // Transient 5xx -> retry once.
                    bool retryable = status == 500 || status == 502
                                  || status == 503 || status == 529;
                    if (retryable && attempt < maxAttempts)
                    {
                        System.Threading.Thread.Sleep(5000);
                        continue;
                    }

                    if (!http.Sent)
                    {
                        result.Error = "Cannot reach Ollama at " + baseUrl +
                            ". Make sure the Ollama server is running ('ollama serve')."
                            + " (" + http.TransportError + ")";
                    }
                    else
                    {
                        result.Error = "Ollama HTTP " + status
                            + " - " + Shorten(body.Length > 0 ? body : "(empty response)");
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    result.Error = "Ollama request failed: " + ex.Message;
                    return result;
                }
            }
            return result;
        }

        /// <summary>
        /// Pull message.content + usage/timing fields out of a non-stream
        /// Ollama response. total_duration is in nanoseconds.
        /// </summary>
        private static void ParseSuccess(JavaScriptSerializer ser, string respText, AiResult result)
        {
            var root = ser.DeserializeObject(respText) as Dictionary<string, object>;
            if (root == null)
            {
                result.Error = "Unexpected Ollama response: " + Shorten(respText);
                return;
            }

            object msgObj;
            if (root.TryGetValue("message", out msgObj))
            {
                var msg = msgObj as Dictionary<string, object>;
                object content;
                if (msg != null && msg.TryGetValue("content", out content))
                {
                    result.Text = Convert.ToString(content);
                    result.Ok = result.Text.Length > 0;
                    if (!result.Ok) result.Error = "Ollama returned an empty reply.";
                }
            }

            // Usage / timing. All fields are optional - older Ollama versions
            // and some upstream models don't return all of them.
            result.InputTokens = AsInt(root, "prompt_eval_count");
            result.OutputTokens = AsInt(root, "eval_count");
            long ns = AsLong(root, "total_duration");
            if (ns > 0) result.DurationMs = ns / 1_000_000;

            if (!result.Ok && result.Error.Length == 0)
            {
                result.Error = "Unexpected Ollama response: " + Shorten(respText);
            }
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

        private static long AsLong(Dictionary<string, object> d, string k)
        {
            object v;
            if (d.TryGetValue(k, out v) && v != null)
            {
                try { return Convert.ToInt64(v); } catch { return 0; }
            }
            return 0;
        }

        private static string Shorten(string s)
        {
            if (string.IsNullOrEmpty(s)) return "(empty)";
            return s.Length > 300 ? s.Substring(0, 300) + "..." : s;
        }
    }
}
