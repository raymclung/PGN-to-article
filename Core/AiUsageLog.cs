using System;
using System.Globalization;
using System.IO;

namespace ChessArticle
{
    /// <summary>
    /// Records what each model call cost, appended as CSV so a run can be
    /// audited afterwards. The original wrote to a database; a standalone tool
    /// writes to a file instead.
    ///
    /// Set <c>CHESSARTICLE_USAGELOG</c> to choose the path. Logging is skipped
    /// entirely when it is empty, and a failure to write is never allowed to
    /// break article generation.
    /// </summary>
    public static class AiUsageLog
    {
        private static readonly object Gate = new object();

        /// <summary>
        /// Estimated cost in USD. Per-million-token prices come from
        /// configuration, so they can be kept current without touching code.
        /// </summary>
        public static decimal ComputeUsd(string provider, string model,
            int inputTokens, int outputTokens)
        {
            decimal inPerM = Dec(Secrets.Get(provider + "InputUsdPerM", "0"));
            decimal outPerM = Dec(Secrets.Get(provider + "OutputUsdPerM", "0"));
            if (inPerM == 0m && outPerM == 0m) return 0m;

            return (inputTokens / 1_000_000m) * inPerM
                 + (outputTokens / 1_000_000m) * outPerM;
        }

        public static void Append(string provider, string model,
            string articleSlug, int jobId,
            int inputTokens, int outputTokens, long durationMs,
            bool ok, string errorMessage)
        {
            string path = Secrets.Get("UsageLog", "");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                decimal usd = ComputeUsd(provider, model, inputTokens, outputTokens);
                string row = string.Join(",",
                    DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    Csv(provider), Csv(model), Csv(articleSlug),
                    jobId.ToString(CultureInfo.InvariantCulture),
                    inputTokens.ToString(CultureInfo.InvariantCulture),
                    outputTokens.ToString(CultureInfo.InvariantCulture),
                    durationMs.ToString(CultureInfo.InvariantCulture),
                    usd.ToString("0.######", CultureInfo.InvariantCulture),
                    ok ? "ok" : "error",
                    Csv(errorMessage));

                lock (Gate)
                {
                    bool fresh = !File.Exists(path);
                    using (var w = new StreamWriter(path, append: true))
                    {
                        if (fresh)
                            w.WriteLine("utc,provider,model,slug,jobId,inputTokens,outputTokens,durationMs,usd,status,error");
                        w.WriteLine(row);
                    }
                }
            }
            catch
            {
                // Auditing is a convenience — never let it fail a run.
            }
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return s.IndexOf(',') >= 0 ? "\"" + s + "\"" : s;
        }

        private static decimal Dec(string s)
        {
            decimal d;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d) ? d : 0m;
        }
    }
}
