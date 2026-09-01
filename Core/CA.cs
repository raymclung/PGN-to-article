namespace ChessArticle
{
    /// <summary>
    /// Configuration key names used by this tool.
    /// uses, so we don't have to modify the shared library.
    ///
    /// Read at runtime with:
    ///     Utils.getAppSettings(CA.AppSettings.GeminiApiKey).ToString()
    ///
    /// Setting values live in App_Data/AppSettings/Application_ChessArticle.xml
    /// (or Application.xml). AppManager.Init() loads them at startup.
    /// </summary>
    public static class CA
    {
        public static class AppSettings
        {
            // ----- AI provider selection -----
            public const string DefaultAiProvider  = "DefaultAiProvider";

            // ----- Ollama (local LLM) -----
            public const string OllamaUrl          = "OllamaUrl";
            public const string OllamaModel        = "OllamaModel";
            public const string SeaLionModel       = "SeaLionModel";

            // ----- Gemini (cloud LLM) -----
            public const string GeminiApiKey       = "GeminiApiKey";
            public const string GeminiModel        = "GeminiModel";
            public const string GeminiImageModel   = "GeminiImageModel";
            public const string AutoGenerateImage  = "AutoGenerateImage";

            // ----- Claude / Anthropic (cloud LLM) -----
            // ClaudeApiKey: server secret, lives in App_Data XML.
            // ClaudeModel: e.g. "claude-sonnet-4-5" (default), or
            // "claude-opus-4-5" / "claude-haiku-4-5".
            // ClaudeInputUsdPerM / ClaudeOutputUsdPerM: USD price per 1M
            // tokens, used to compute per-call cost for the AI usage log.
            // Defaults (Sonnet 4.5 list): 3 / 15.
            public const string ClaudeApiKey       = "ClaudeApiKey";
            public const string ClaudeModel        = "ClaudeModel";
            public const string ClaudeInputUsdPerM  = "ClaudeInputUsdPerM";
            public const string ClaudeOutputUsdPerM = "ClaudeOutputUsdPerM";

            // ----- Engine analysis (Stockfish + analyze_pgn.py) -----
            public const string StockfishPath      = "StockfishPath";
            public const string PythonPath         = "PythonPath";
            public const string EngineDepth        = "EngineDepth";

            // ----- optional player-rating source -----
            // ----- Background orchestrator -----
            public const string OrchestratorToken = "OrchestratorToken";

            // ----- Public REST API (/api/articles.ashx, /api/article-pgn.ashx) -----
            public const string ApiAllowedOrigin = "ApiAllowedOrigin";
            public const string ApiBearerToken   = "ApiBearerToken";
        }
    }
}
