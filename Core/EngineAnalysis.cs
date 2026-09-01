using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ChessArticle
{
    /// <summary>One turning point found by the engine.</summary>
    public class CriticalMoment
    {
        public string MoveLabel { get; set; }
        public string Side { get; set; }
        public string Best { get; set; }
        public string PvLine { get; set; }      // Engine's principal variation in SAN
        public string EvalBefore { get; set; }
        public string EvalAfter { get; set; }
        public double LossPawns { get; set; }
        public string Classification { get; set; }
        public int? Clock { get; set; }

        public CriticalMoment()
        {
            MoveLabel = ""; Side = ""; Best = ""; PvLine = "";
            EvalBefore = ""; EvalAfter = ""; Classification = "";
        }
    }

    /// <summary>One board diagram (inline SVG) produced by the engine helper.</summary>
    public class EngineDiagram
    {
        public int Num { get; set; }
        public int Ply { get; set; }            // half-move index, for Viewer deep-link
        public string MoveLabel { get; set; }
        public string Eval { get; set; }
        public string Kind { get; set; }
        public string Caption { get; set; }
        public string Svg { get; set; }

        public EngineDiagram()
        {
            MoveLabel = ""; Eval = ""; Kind = ""; Caption = ""; Svg = "";
        }
    }

    /// <summary>Outcome of running the engine-analysis helper on one game.</summary>
    public class EngineResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public bool Is960 { get; set; }
        public List<CriticalMoment> Criticals { get; set; }
        public List<EngineDiagram> Diagrams { get; set; }

        // Per-ply data (length = plies + 1, 0 = initial position).
        // Used by the Viewer page to replay the game with the eval bar.
        public List<string> Fens { get; set; }
        public List<int> Evals { get; set; }
        public List<int> Mates { get; set; }
        public List<string> Sans { get; set; }
        public List<string> LastMoves { get; set; }
        public List<string> Bests { get; set; }
        public List<string> PvLines { get; set; }
        public List<string> Svgs { get; set; }
        public List<string> SvgsB { get; set; }
        public string Defs { get; set; }

        public EngineResult()
        {
            Ok = false;
            Error = "";
            Criticals = new List<CriticalMoment>();
            Diagrams = new List<EngineDiagram>();
            Fens = new List<string>();
            Evals = new List<int>();
            Mates = new List<int>();
            Sans = new List<string>();
            LastMoves = new List<string>();
            Bests = new List<string>();
            PvLines = new List<string>();
            Svgs = new List<string>();
            SvgsB = new List<string>();
            Defs = "";
        }
    }

    /// <summary>
    /// Runs tools/analyze_pgn.py (python-chess + Stockfish) as a subprocess to
    /// evaluate a game, find its turning points and render diagram positions.
    /// The C# app is the gateway; Python only does the "Engine Analysis" stage.
    /// </summary>
    public static class EngineAnalysis
    {
        public static EngineResult Run(string pgnText, string language = "en")
        {
            var result = new EngineResult();

            string python = Setting(CA.AppSettings.PythonPath, "python");
            string depth = Setting(CA.AppSettings.EngineDepth, "16");
            // Paths come from configuration so the tool runs anywhere, not
            // just inside a web application.
            string siteRoot = Setting("ContentRoot", AppContext.BaseDirectory);
            string script = Setting("AnalyzeScript",
                Path.Combine(siteRoot, "tools", "analyze_pgn.py"));

            string enginePath = Setting(CA.AppSettings.StockfishPath, @"..\engine\stockfish.exe");
            if (!Path.IsPathRooted(enginePath))
                enginePath = Path.GetFullPath(Path.Combine(siteRoot, enginePath));

            if (!File.Exists(script))
            {
                result.Error = "Engine helper not found: " + script;
                return result;
            }
            if (!File.Exists(enginePath))
            {
                result.Error = "Stockfish engine not found: " + enginePath;
                return result;
            }

            string pgnFile = Path.Combine(Path.GetTempPath(),
                "chessart_" + Guid.NewGuid().ToString("N") + ".pgn");
            try
            {
                File.WriteAllText(pgnFile, pgnText ?? "", Encoding.UTF8);

                var psi = new ProcessStartInfo
                {
                    FileName = python,
                    Arguments = "\"" + script + "\" --pgn \"" + pgnFile + "\" --engine \""
                        + enginePath + "\" --depth " + depth,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };
                // Pass the chosen article language down so analyze_pgn.py
                // can localise human-facing strings (diagram captions, etc).
                // Chess notation stays international regardless.
                psi.EnvironmentVariables["CHESSARTICLE_LANG"] =
                    string.IsNullOrWhiteSpace(language) ? "en" : language.ToLowerInvariant();

                string stdout, stderr;
                using (var proc = new Process { StartInfo = psi })
                {
                    proc.Start();
                    Task<string> outTask = proc.StandardOutput.ReadToEndAsync();
                    Task<string> errTask = proc.StandardError.ReadToEndAsync();

                    if (!proc.WaitForExit(540000)) // 9 minutes
                    {
                        try { proc.Kill(); } catch { }
                        result.Error = "Engine analysis timed out (over 9 minutes).";
                        return result;
                    }
                    stdout = outTask.Result;
                    stderr = errTask.Result;
                }

                if (string.IsNullOrWhiteSpace(stdout))
                {
                    result.Error = "Engine helper produced no output. "
                        + (string.IsNullOrWhiteSpace(stderr)
                            ? "Is Python installed and on the server's PATH? "
                              + "Set the full path in Web.config / PythonPath if needed."
                            : stderr.Trim());
                    return result;
                }

                Parse(stdout, result);
            }
            catch (Exception ex)
            {
                result.Error = "Could not run engine analysis: " + ex.Message;
            }
            finally
            {
                try { if (File.Exists(pgnFile)) File.Delete(pgnFile); }
                catch { /* ignore temp cleanup failure */ }
            }
            return result;
        }

        private static void Parse(string stdout, EngineResult result)
        {
            var ser = new JavaScriptSerializer { MaxJsonLength = 64 * 1024 * 1024 };

            string json = stdout.Trim();
            int open = json.IndexOf('{');
            int close = json.LastIndexOf('}');
            if (open >= 0 && close > open) json = json.Substring(open, close - open + 1);

            Dictionary<string, object> root;
            try
            {
                root = ser.DeserializeObject(json) as Dictionary<string, object>;
            }
            catch (Exception ex)
            {
                result.Error = "Could not read engine output: " + ex.Message;
                return;
            }
            if (root == null)
            {
                result.Error = "Engine output was not a JSON object.";
                return;
            }

            if (!Bool(root, "ok"))
            {
                result.Error = Str(root, "error");
                if (result.Error.Length == 0) result.Error = "Engine analysis failed.";
                return;
            }

            result.Is960 = Bool(root, "is960");

            object critObj;
            if (root.TryGetValue("criticals", out critObj))
            {
                var arr = critObj as object[];
                if (arr != null)
                {
                    foreach (object o in arr)
                    {
                        var d = o as Dictionary<string, object>;
                        if (d == null) continue;
                        result.Criticals.Add(new CriticalMoment
                        {
                            MoveLabel = Str(d, "move_label"),
                            Side = Str(d, "side"),
                            Best = Str(d, "best"),
                            PvLine = Str(d, "pvline"),
                            EvalBefore = Str(d, "eval_before"),
                            EvalAfter = Str(d, "eval_after"),
                            LossPawns = Dbl(d, "loss_pawns"),
                            Classification = Str(d, "classification"),
                            Clock = IntOrNull(d, "clock"),
                        });
                    }
                }
            }

            object diagObj;
            if (root.TryGetValue("diagrams", out diagObj))
            {
                var arr = diagObj as object[];
                if (arr != null)
                {
                    foreach (object o in arr)
                    {
                        var d = o as Dictionary<string, object>;
                        if (d == null) continue;
                        result.Diagrams.Add(new EngineDiagram
                        {
                            Num = (int)Dbl(d, "num"),
                            Ply = (int)Dbl(d, "ply"),
                            MoveLabel = Str(d, "move_label"),
                            Eval = Str(d, "eval"),
                            Kind = Str(d, "kind"),
                            Caption = Str(d, "caption"),
                            Svg = Str(d, "svg"),
                        });
                    }
                }
            }

            object pliesObj;
            if (root.TryGetValue("plies", out pliesObj))
            {
                var arr = pliesObj as object[];
                if (arr != null)
                {
                    foreach (object o in arr)
                    {
                        var d = o as Dictionary<string, object>;
                        if (d == null) continue;
                        result.Fens.Add(Str(d, "fen"));
                        result.Evals.Add((int)Dbl(d, "cp"));
                        result.Mates.Add((int)Dbl(d, "mate"));
                        result.Sans.Add(Str(d, "san"));
                        result.LastMoves.Add(Str(d, "lastmove_uci"));
                        result.Bests.Add(Str(d, "best"));
                        result.PvLines.Add(Str(d, "pvline"));
                        result.Svgs.Add(Str(d, "svg"));
                        result.SvgsB.Add(Str(d, "svgb"));
                    }
                }
            }
            result.Defs = Str(root, "defs");

            result.Ok = true;
        }

        /// <summary>
        /// Chains Secrets.Get (env + AppManager XML + Web.config) with a
        /// caller-supplied fallback. Engine settings live in the platform
        /// XML loader since 2026-05-25, but the legacy Web.config path is
        /// still honoured by Secrets.Get as a last resort.
        /// </summary>
        private static string Setting(string key, string fallback)
        {
            return Secrets.Get(key, fallback);
        }

        private static string Str(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) && v != null ? Convert.ToString(v) : "";
        }

        private static bool Bool(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) && v is bool && (bool)v;
        }

        private static double Dbl(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) && v != null ? Convert.ToDouble(v) : 0.0;
        }

        private static int? IntOrNull(Dictionary<string, object> d, string key)
        {
            object v;
            if (d.TryGetValue(key, out v) && v != null)
                return Convert.ToInt32(v);
            return null;
        }
    }
}
