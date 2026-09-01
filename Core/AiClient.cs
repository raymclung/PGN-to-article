using System;

namespace ChessArticle
{
    /// <summary>Raw result of one AI generation call (provider-agnostic).</summary>
    public class AiResult
    {
        public bool Ok { get; set; }
        public string Text { get; set; }
        public string Error { get; set; }

        // Usage / timing metadata. Populated best-effort by each provider;
        // a zero means the provider didn't report it, not "took zero time".
        public int InputTokens { get; set; }     // prompt tokens
        public int OutputTokens { get; set; }    // completion tokens
        public long DurationMs { get; set; }     // server-side wall time

        public AiResult()
        {
            Ok = false;
            Text = "";
            Error = "";
        }

        /// <summary>"60 tok/s" or empty when we don't have enough data.</summary>
        public string TokensPerSecond()
        {
            if (OutputTokens <= 0 || DurationMs <= 0) return "";
            double tps = OutputTokens * 1000.0 / DurationMs;
            return tps.ToString("0") + " tok/s";
        }
    }

    /// <summary>Optional per-call sampling overrides. null = use the provider's
    /// tuned defaults from Web.config / built-in. Pass to AiClient.Generate
    /// when one task needs a different temperature (e.g. a reviewer pass that
    /// wants deterministic output instead of the creative 0.5).</summary>
    public class AiOptions
    {
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }     // num_predict for Ollama
        public int? NumCtx { get; set; }        // Ollama context window

        public static AiOptions Default { get { return new AiOptions(); } }

        /// <summary>Deterministic preset for review / extraction tasks.</summary>
        public static AiOptions Strict
        {
            get { return new AiOptions { Temperature = 0.2 }; }
        }
    }

    /// <summary>
    /// Dispatches an article-generation request to the chosen AI provider.
    /// Providers:
    ///   "Ollama"  - local server, default model from AppSettings OllamaModel
    ///   "Gemini"  - Google API (cloud)
    ///   "Claude"  - Anthropic API (cloud, ClaudeModel = sonnet 4.5 default)
    ///   "SeaLion" - local Ollama with the SeaLion model (multilingual,
    ///               optimised for Southeast Asian languages incl. Indonesian).
    ///               Same Ollama endpoint, different model name from
    ///               AppSettings SeaLionModel.
    ///
    /// Every call also writes one row to ArticleAiLog (cost + token audit).
    /// Failure to log never blocks the user-facing AI call.
    /// </summary>
    public static class AiClient
    {
        public const string ProviderOllama  = "Ollama";
        public const string ProviderGemini  = "Gemini";
        public const string ProviderClaude  = "Claude";
        public const string ProviderSeaLion = "SeaLion";

        public static AiResult Generate(string provider, string systemPrompt, string userPrompt)
        {
            return Generate(provider, systemPrompt, userPrompt, null);
        }

        public static AiResult Generate(string provider, string systemPrompt, string userPrompt,
            AiOptions options)
        {
            return Generate(provider, systemPrompt, userPrompt, options, null, 0);
        }

        /// <summary>
        /// Full-arity entry point. articleSlug + jobId are tagged onto the
        /// audit log row so we can correlate cost against a specific article
        /// or background job later. Both are optional.
        /// </summary>
        public static AiResult Generate(string provider, string systemPrompt, string userPrompt,
            AiOptions options, string articleSlug, int jobId)
        {
            AiResult r;
            string modelUsed;

            if (string.Equals(provider, ProviderGemini, StringComparison.OrdinalIgnoreCase))
            {
                r = GeminiClient.Generate(systemPrompt, userPrompt, options);
                modelUsed = Secrets.Get(CA.AppSettings.GeminiModel, "gemini-2.5-flash");
            }
            else if (string.Equals(provider, ProviderClaude, StringComparison.OrdinalIgnoreCase))
            {
                r = ClaudeClient.Generate(systemPrompt, userPrompt, options);
                modelUsed = Secrets.Get(CA.AppSettings.ClaudeModel, "claude-sonnet-4-5");
            }
            else if (string.Equals(provider, ProviderSeaLion, StringComparison.OrdinalIgnoreCase))
            {
                r = OllamaClient.Generate(systemPrompt, userPrompt, CA.AppSettings.SeaLionModel, options);
                modelUsed = Secrets.Get(CA.AppSettings.SeaLionModel, "");
            }
            else
            {
                // Default and "Ollama" both go to the local Ollama server.
                r = OllamaClient.Generate(systemPrompt, userPrompt, CA.AppSettings.OllamaModel, options);
                modelUsed = Secrets.Get(CA.AppSettings.OllamaModel, "");
                provider = ProviderOllama;
            }

            AiUsageLog.Append(
                provider:      provider,
                model:         modelUsed,
                articleSlug:   articleSlug,
                jobId:         jobId,
                inputTokens:   r.InputTokens,
                outputTokens:  r.OutputTokens,
                durationMs:    r.DurationMs,
                ok:            r.Ok,
                errorMessage:  r.Error);

            return r;
        }
    }
}
