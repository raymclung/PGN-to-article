using System;
using System.Collections.Generic;
using System.IO;
using ChessArticle;

/// <summary>
/// Turns a PGN file into a written chess article.
///
///   pgn-to-article game.pgn -o article.md
///
/// The game is parsed, optionally analysed by Stockfish for critical moments,
/// then handed to a language model together with a prompt describing how the
/// article should read.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        var opt = ParseArgs(args);
        if (opt == null) { Usage(); return 1; }

        if (!File.Exists(opt.Input))
        {
            Console.Error.WriteLine("PGN not found: " + opt.Input);
            return 1;
        }

        // ── 1. Parse ────────────────────────────────────────────────
        string pgnText = File.ReadAllText(opt.Input);
        PgnGame game = PgnParser.Parse(pgnText);
        if (game == null || game.Moves.Count == 0)
        {
            Console.Error.WriteLine("No moves found in " + opt.Input);
            return 1;
        }
        Console.Error.WriteLine($"[1/4] Parsed {game.White} vs {game.Black} — {game.Moves.Count} half-moves");

        // ── 2. Engine analysis (optional) ───────────────────────────
        EngineResult engine = null;
        if (!opt.NoEngine)
        {
            Console.Error.WriteLine("[2/4] Running engine analysis...");
            try
            {
                engine = EngineAnalysis.Run(pgnText);
                if (engine != null && !string.IsNullOrEmpty(engine.Error))
                    Console.Error.WriteLine("      engine: " + engine.Error + " (continuing without it)");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("      engine unavailable: " + ex.Message + " (continuing without it)");
            }
        }
        else
        {
            Console.Error.WriteLine("[2/4] Engine analysis skipped (--no-engine)");
        }

        // ── 3. Build the prompt ─────────────────────────────────────
        string system = PromptBuilder.SystemPrompt();
        string user = PromptBuilder.UserPrompt(
            game, engine, opt.Context, opt.WordsLow, opt.WordsHigh,
            null, null, opt.Language);

        if (opt.PromptOnly)
        {
            Write(opt.Output, system + "\n\n---\n\n" + user);
            Console.Error.WriteLine("[3/4] Prompt written (--prompt-only), no model called");
            return 0;
        }
        Console.Error.WriteLine($"[3/4] Prompt built — {user.Length:N0} characters");

        // ── 4. Ask the model ────────────────────────────────────────
        Console.Error.WriteLine($"[4/4] Asking {opt.Provider}...");
        AiResult result;
        try
        {
            result = AiClient.Generate(opt.Provider, system, user);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Model call failed: " + ex.Message);
            return 2;
        }

        if (result == null || !string.IsNullOrEmpty(result.Error))
        {
            Console.Error.WriteLine("Model returned an error: " +
                (result == null ? "no result" : result.Error));
            return 2;
        }

        // The model answers with a JSON envelope: title, body_markdown, image
        // prompts, social caption, tags. Unless --json was asked for, unwrap it
        // and keep the article itself.
        string raw = result.Text ?? "";
        string body = opt.Json ? raw : Unwrap(raw);

        body = Scrubber.ScrubBody(body);
        if (opt.Html) body = MarkdownLite.ToHtml(body, new List<ArticleDiagram>());

        Write(opt.Output, body);
        Console.Error.WriteLine($"      done — {body.Length:N0} characters written");
        return 0;
    }

    private static void Write(string path, string text)
    {
        if (string.IsNullOrEmpty(path)) Console.Out.Write(text);
        else File.WriteAllText(path, text);
    }

    /// <summary>
    /// Pulls the article out of the JSON envelope the model returns, putting
    /// the title back on top as a heading. Returns the input unchanged when it
    /// is not JSON — a model that answered in plain Markdown still works.
    /// </summary>
    private static string Unwrap(string raw)
    {
        string t = (raw ?? "").Trim();
        int open = t.IndexOf('{');
        int close = t.LastIndexOf('}');
        if (open < 0 || close <= open) return raw;

        try
        {
            using (var doc = System.Text.Json.JsonDocument.Parse(t.Substring(open, close - open + 1)))
            {
                System.Text.Json.JsonElement root = doc.RootElement;
                System.Text.Json.JsonElement bodyEl;
                if (!root.TryGetProperty("body_markdown", out bodyEl)) return raw;

                string body = bodyEl.GetString() ?? "";
                System.Text.Json.JsonElement titleEl;
                string title = root.TryGetProperty("title", out titleEl) ? (titleEl.GetString() ?? "") : "";

                return string.IsNullOrEmpty(title) ? body : "# " + title + "\n\n" + body;
            }
        }
        catch
        {
            return raw;   // malformed JSON: hand back whatever the model said
        }
    }

    private sealed class Options
    {
        public string Input = "";
        public string Output = "";
        public string Provider = "ollama";
        public string Language = "id";
        public string Context = "";
        public int WordsLow = 600;
        public int WordsHigh = 900;
        public bool NoEngine;
        public bool PromptOnly;
        public bool Html;
        public bool Json;
    }

    private static Options ParseArgs(string[] args)
    {
        if (args.Length == 0) return null;
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string Next() => (i + 1 < args.Length) ? args[++i] : "";
            switch (a)
            {
                case "-o":
                case "--out": o.Output = Next(); break;
                case "--provider": o.Provider = Next(); break;
                case "--lang": o.Language = Next(); break;
                case "--context": o.Context = Next(); break;
                case "--words":
                    string[] w = Next().Split('-');
                    if (w.Length == 2)
                    {
                        int lo, hi;
                        if (int.TryParse(w[0], out lo)) o.WordsLow = lo;
                        if (int.TryParse(w[1], out hi)) o.WordsHigh = hi;
                    }
                    break;
                case "--no-engine": o.NoEngine = true; break;
                case "--prompt-only": o.PromptOnly = true; break;
                case "--html": o.Html = true; break;
                case "--json": o.Json = true; break;
                case "-h":
                case "--help": return null;
                default:
                    if (a.StartsWith("-")) { Console.Error.WriteLine("Unknown option: " + a); return null; }
                    o.Input = a;
                    break;
            }
        }
        return string.IsNullOrEmpty(o.Input) ? null : o;
    }

    private static void Usage()
    {
        Console.Error.WriteLine(@"
pgn-to-article — turn a chess game into a written article

  pgn-to-article <game.pgn> [options]

Options
  -o, --out <file>     write the article here (default: stdout)
  --provider <name>    ollama | gemini | claude          (default: ollama)
  --lang <id|en>       language of the article           (default: id)
  --context <text>     extra context: event, round, stakes
  --words <low-high>   target length                     (default: 600-900)
  --no-engine          skip Stockfish analysis
  --prompt-only        print the prompt, do not call a model
  --html               render the result as HTML instead of Markdown
  --json               keep the raw JSON envelope (title, tags, image prompts)

Configuration is read from environment variables, never from files in this
repository:

  CHESSARTICLE_OLLAMAURL        default http://127.0.0.1:11434
  CHESSARTICLE_OLLAMAMODEL
  CHESSARTICLE_GEMINIAPIKEY
  CHESSARTICLE_CLAUDEAPIKEY
  CHESSARTICLE_STOCKFISHPATH
  CHESSARTICLE_PYTHONPATH
  CHESSARTICLE_CONTENTROOT      where tools/analyze_pgn.py lives
");
    }
}
