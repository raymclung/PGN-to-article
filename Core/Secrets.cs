using System;

namespace ChessArticle
{
    /// <summary>
    /// Configuration lookup. Every value is read from an environment variable
    /// so that nothing sensitive ever has to live in a file inside the repo.
    ///
    /// A key such as <c>GeminiApiKey</c> is read from
    /// <c>CHESSARTICLE_GEMINIAPIKEY</c>; the plain key name is also accepted as
    /// a fallback. When neither is set the supplied default is returned.
    ///
    ///   PowerShell : $env:CHESSARTICLE_GEMINIAPIKEY = "..."
    ///   Bash       : export CHESSARTICLE_GEMINIAPIKEY="..."
    /// </summary>
    public static class Secrets
    {
        private const string Prefix = "CHESSARTICLE_";

        public static string Get(string key, string fallback = "")
        {
            if (string.IsNullOrEmpty(key)) return fallback;

            string v = Environment.GetEnvironmentVariable(Prefix + key.ToUpperInvariant());
            if (!string.IsNullOrEmpty(v)) return v;

            v = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(v)) return v;

            return fallback;
        }

        public static int GetInt(string key, int fallback)
        {
            int n;
            return int.TryParse(Get(key, ""), out n) ? n : fallback;
        }

        public static bool GetBool(string key, bool fallback)
        {
            bool b;
            return bool.TryParse(Get(key, ""), out b) ? b : fallback;
        }
    }
}
