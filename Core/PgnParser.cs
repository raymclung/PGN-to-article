using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChessArticle
{
    /// <summary>
    /// Parses PGN text into a PgnGame. Reads the seven-tag headers, the SAN move
    /// list, and any inline [%eval ...] / [%clk ...] annotations from comments.
    /// Does NOT validate chess legality - it treats the movetext as already valid.
    /// </summary>
    public static class PgnParser
    {
        // Header line: [Tag "Value"]
        private static readonly Regex HeaderRx =
            new Regex("^\\s*\\[\\s*(\\w+)\\s+\"(.*)\"\\s*\\]\\s*$");

        // A SAN move: piece moves, pawn moves, castling, promotion, check/mate.
        private static readonly Regex SanRx =
            new Regex("^(O-O-O|O-O|[KQRBN]?[a-h]?[1-8]?x?[a-h][1-8](=[QRBN])?)[+#]?$");

        // Movetext token stream.
        private static readonly Regex TokenRx =
            new Regex("\\{[^}]*\\}|\\d+\\.(\\.\\.)?|\\$\\d+|1-0|0-1|1/2-1/2|\\*|[^\\s{}()]+");

        public static PgnGame Parse(string pgn)
        {
            var game = new PgnGame();
            if (string.IsNullOrWhiteSpace(pgn)) return game;

            string text = pgn.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] lines = text.Split('\n');

            // --- Headers ---
            int firstMoveLine = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Match h = HeaderRx.Match(line);
                if (h.Success)
                {
                    game.Headers[h.Groups[1].Value] = h.Groups[2].Value;
                    firstMoveLine = i + 1;
                }
                else if (line.Trim().Length == 0)
                {
                    firstMoveLine = i + 1;
                }
                else
                {
                    break; // movetext starts here
                }
            }

            string movetext = string.Join(" ", lines, firstMoveLine, lines.Length - firstMoveLine);

            // Drop variations (innermost parentheses, repeatedly).
            string prev;
            do
            {
                prev = movetext;
                movetext = Regex.Replace(movetext, "\\([^()]*\\)", " ");
            } while (movetext != prev);

            // --- Walk the token stream ---
            bool whiteToMove = true;
            int fullMove = 1;
            PgnMove lastMove = null;

            foreach (Match tok in TokenRx.Matches(movetext))
            {
                string t = tok.Value.Trim();
                if (t.Length == 0) continue;

                if (t.StartsWith("{"))
                {
                    if (lastMove != null) ApplyComment(lastMove, t);
                    continue;
                }
                if (t == "1-0" || t == "0-1" || t == "1/2-1/2" || t == "*")
                {
                    game.Result = t;
                    continue;
                }
                if (t.StartsWith("$")) continue;                 // NAG
                if (Regex.IsMatch(t, "^\\d+\\.(\\.\\.)?$")) continue; // move number

                if (SanRx.IsMatch(t))
                {
                    var m = new PgnMove
                    {
                        San = t,
                        IsWhite = whiteToMove,
                        MoveNumber = fullMove
                    };
                    game.Moves.Add(m);
                    lastMove = m;

                    if (!whiteToMove) fullMove++;
                    whiteToMove = !whiteToMove;
                }
                // anything else (stray token) is ignored
            }

            if (game.Result.Length == 0) game.Result = game.Header("Result");

            // Optional roster enrichment. When a resolver is registered it may
            // fill in real names, titles, and teams so the prompt can say
            // "Jane Doe (Acme Club)" instead of a raw handle. Best-effort: any
            // failure simply leaves the roster fields empty.
            if (RosterResolver != null)
            {
                try { RosterResolver(game); }
                catch { /* roster data is optional - never fail the parse */ }
            }

            return game;
        }

        /// <summary>
        /// Optional hook for enriching a parsed game with roster data — real
        /// names, titles, teams, ratings. Left unset, parsing works entirely
        /// from the PGN headers.
        /// </summary>
        public static Action<PgnGame> RosterResolver { get; set; }

        private static void ApplyComment(PgnMove move, string comment)
        {
            // [%eval 0.24]  or  [%eval #-3]  (mate)
            Match e = Regex.Match(comment, "%eval\\s+(#?-?\\d+(?:\\.\\d+)?)");
            if (e.Success)
            {
                string raw = e.Groups[1].Value;
                double val;
                if (raw.StartsWith("#"))
                {
                    // Mate score - clamp to a large pawn value, signed.
                    bool neg = raw.Contains("-");
                    val = neg ? -100.0 : 100.0;
                    move.HasEval = true;
                    move.Eval = val;
                }
                else if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                {
                    move.HasEval = true;
                    move.Eval = val;
                }
            }

            // [%clk 0:01:23]
            Match c = Regex.Match(comment, "%clk\\s+(\\d+:\\d+:\\d+(?:\\.\\d+)?)");
            if (c.Success) move.Clock = c.Groups[1].Value;
        }
    }
}
