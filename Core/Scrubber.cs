using System;
using System.Text.RegularExpressions;

namespace ChessArticle
{
    /// <summary>
    /// Deterministic style fixer for AI-generated article text. Cheap cliche
    /// words ("stunning", "catastrophic", "masterclass") slip past even a
    /// strict system prompt; this strips them so editors don't have to.
    ///
    /// Mirrors tools/sample_article.py's REPLACEMENTS list - keep them in
    /// sync when adding new banned words.
    ///
    /// Apply to: Title, BodyMarkdown, SocialCaption, MetaDescription.
    /// Don't apply to: Tags (already short), ImagePrompt* (separate channel),
    /// engine-derived fields (Pgn/Fens/Sans) - those are factual, not prose.
    /// </summary>
    public static class Scrubber
    {
        private static readonly Tuple<Regex, string>[] _rules = new[]
        {
            // Drop filler adverbs entirely - they're usually empty intensifiers.
            New(@"\bcatastrophically\b\s*", ""),
            New(@"\bfatally\b\s*",          ""),
            New(@"\bdevastatingly\b\s*",    ""),
            New(@"\bstunningly\b\s*",       ""),
            New(@"\bbrilliantly\b\s*",      ""),
            New(@"\bspectacularly\b\s*",    ""),
            New(@"\bbreathtakingly\b\s*",   ""),
            New(@"\bmasterfully\b\s*",      ""),
            New(@"\bincredibly\b\s*",       ""),
            New(@"\bshockingly\b\s*",       ""),
            New(@"\babsolutely\b\s*",       ""),     // filler intensifier
            New(@"\btruly\b\s*",            ""),     // filler intensifier

            // Verb forms get verb replacements (so "Carlsen Devastates Foe"
            // becomes "Carlsen Outclasses Foe", not the ungrammatical
            // "Carlsen Severe Foe").
            // KNOWN EDGE CASE: "crush" and "stun" can also be NOUNS in chess
            // slang ("a kingside crush" = quick mating attack). The pattern
            // treats them as verbs, so a noun-form title slot may need
            // manual cleanup after a backfill. Body text usage is almost
            // always verbal, so this is rare in practice.
            New(@"\bdevastates\b",                         "outclasses"),
            New(@"\bdevastated\b",                         "outclassed"),
            New(@"\bdevastate\b",                          "outclass"),
            New(@"\bdevastation\b",                        "wreckage"),
            New(@"\bcrushes\b",                            "outplays"),
            New(@"\bcrushed\b",                            "outplayed"),
            New(@"\bcrush\b",                              "outplay"),
            New(@"\bstuns\b",                              "unsettles"),
            New(@"\bstunned\b",                            "unsettled"),
            New(@"\bstun\b",                               "unsettle"),
            // Adjective / noun substitutions.
            New(@"\bcatastroph(?:e|es|ic|ies)\b",          "setback"),
            New(@"\bcrushing(?:ly)?\b",                    "relentless"),
            New(@"\bfatal\b",                              "costly"),
            New(@"\bdevastating\b",                        "severe"),
            New(@"\bstunning\b",                           "precise"),
            New(@"\bbrilliant\b",                          "forced"),
            New(@"\bbrilliance\b",                         "precision"),
            New(@"\bspectacular\b",                        "forced"),
            New(@"\bbreathtaking\b",                       "precise"),
            New(@"\bbreathless\b",                         "riveted"),
            New(@"\bheartbreaking\b",                      "painful"),
            New(@"\bexhilarating\b",                       "sharp"),
            New(@"\bexhilaration\b",                       "rush"),
            New(@"\bmasterclass\b",                        "study"),
            New(@"\bmasterful\b",                          "controlled"),
            New(@"\bincredible\b",                         "striking"),
            New(@"\bshocking\b",                           "abrupt"),
            New(@"\bastonishment\b",                       "surprise"),
            New(@"\bastonishing(?:ly)?\b",                 "abrupt"),
            // Sensational/clickbait noise flagged by the editorial review.
            New(@"\brampage\b",                            "pressure"),
            New(@"\bdazzling(?:ly)?\b",                    "precise"),
            New(@"\bsensational(?:ly)?\b",                 "sharp"),
            New(@"\bjaw-dropping\b",                       "sharp"),
            New(@"\belectrifying\b",                       "sharp"),
            New(@"\bdestruction\b",                        "collapse"),
            New(@"\bannihilation\b",                       "collapse"),
            New(@"\bdemolition\b",                         "collapse"),
            // Drop the over-narration adverbs the prompt also flags.
            New(@"\bseemingly\b\s*",                       ""),
            // Cliched closers - swap the whole phrase.
            New(@"\bclassic for the ages\b",               "memorable game"),
            New(@"\bwill live on in chess history\b",      "will stay in the books"),
            New(@"\bleaves? you in awe\b",                 "rewards a second look"),

            // ------------------------------------------------------------------
            // Bahasa Indonesia rules. Same idea as the English block above:
            // drop filler adverbs, replace overused adjectives/verbs with
            // concrete equivalents. Mirrors the LANGUAGE - HARD RULE prompt
            // block in PromptBuilder.LanguageDirective("id"). These rules
            // share the same regex array - Indonesian words don't collide
            // with English words, so the whole list runs on every article.
            // ------------------------------------------------------------------

            // Drop filler adverbs ("secara X-ly" forms).
            New(@"\bsecara\s+menakjubkan\b\s*",            ""),
            New(@"\bsecara\s+dramatis\b\s*",               ""),
            New(@"\bsecara\s+spektakuler\b\s*",            ""),
            New(@"\bsecara\s+fenomenal\b\s*",              ""),
            New(@"\bsecara\s+brilian\b\s*",                ""),
            New(@"\bdengan\s+brilian\b\s*",                ""),
            New(@"\bdengan\s+menakjubkan\b\s*",            ""),

            // Adjective / noun replacements - prefer concrete over hyped.
            New(@"\bmenakjubkan\b",                        "tepat"),
            New(@"\bmemukau\b",                            "tepat"),
            New(@"\bspektakuler\b",                        "tajam"),
            New(@"\bfenomenal\b",                          "tajam"),
            New(@"\bbrilian\b",                            "presisi"),
            New(@"\bcemerlang\b",                          "presisi"),
            New(@"\bepik\b",                               "berkesan"),
            New(@"\bdahsyat\b",                            "tajam"),
            New(@"\bmenghancurkan\b",                      "tegas"),
            New(@"\bkehancuran\b",                         "keruntuhan"),
            New(@"\bkelas master\b",                       "studi"),
            New(@"\bkelas mahakarya\b",                    "studi"),
            New(@"\bpertarungan epik\b",                   "pertarungan keras"),
            New(@"\bperjalanan emosional\b",               "perjalanan panjang"),
            New(@"\brollercoaster\s+emosi\b",              "alur yang berubah-ubah"),
            // "Fatal" used loosely - reserve for true game-ending moves; the
            // model overuses it. Replace with "mahal" (= costly).
            New(@"\bfatal\b",                              "mahal"),
        };

        // Numbered or bulleted lessons with bold lead-ins, e.g.:
        //   "1.  **Beware of Premature Pawn Pushes:** Carlsen's 18.d4..."
        // -> "Beware of Premature Pawn Pushes. Carlsen's 18.d4..."
        private static readonly Regex _numberedBold = new Regex(
            @"^\s*\d+\.\s*\*\*([^*\n]+?)[:\.]?\*\*\s*",
            RegexOptions.Multiline);
        private static readonly Regex _bulletBold = new Regex(
            @"^\s*[-*]\s+\*\*([^*\n]+?)[:\.]?\*\*\s*",
            RegexOptions.Multiline);
        // Standalone bold-header line, e.g. "**Lessons for Club Players:**".
        private static readonly Regex _standaloneBold = new Regex(
            @"^\s*\*\*[^*\n]{3,80}[:\.]?\*\*\s*$",
            RegexOptions.Multiline);
        // "a innocuous" -> "an innocuous": vowel-sound fix. Skip "useful"
        // (yoo-sound) and similar by NOT including those exceptions - we
        // accept a few false positives over false negatives.
        private static readonly Regex _aVowel = new Regex(
            @"\b([Aa])\s+(?=(?:a|e|i|o|u|hour|honest|honor)[a-z])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Cliched closings the editorial review flagged - strip the whole match.
        private static readonly Tuple<Regex, string>[] _closingCliches = new[]
        {
            // Sentence-leading patterns eat the leading subject + the period
            // so we don't leave dangling fragments behind.
            Tuple.Create(new Regex(
                @"(?:^|\s+)(?:It|This|That) was an? (?:stark|painful|sobering) reminder of [^.]{5,120}\.\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
            Tuple.Create(new Regex(
                @"(?:^|\s+)(?:It|This|That) (?:will (?:surely )?be remembered|is a (?:moment|game) for the ages)[^.]{0,80}\.\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
            Tuple.Create(new Regex(
                @"(?:^|\s+)an? (?:stark|painful|sobering) reminder of [^.]{5,120}\.\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
            Tuple.Create(new Regex(
                @"(?:^|\s+)an? reminder of why we (?:love|play) this game\.?\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
            Tuple.Create(new Regex(
                @"(?:^|\s+)an? (?:moment|game) for the ages\.?\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),

            // Bahasa Indonesia closing cliches - same pattern, the same
            // editorial review flagged them as filler.
            Tuple.Create(new Regex(
                @"(?:^|\s+)(?:Ini|Itu|Partai ini) (?:akan|pasti) (?:dikenang|diingat)(?:\s+sepanjang(?:\s+masa|\s+waktu))?[^.]{0,80}\.\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
            Tuple.Create(new Regex(
                @"(?:^|\s+)(?:Ini|Itu) (?:menjadi|adalah) pengingat(?:\s+penting)? (?:bahwa|akan)[^.]{5,120}\.\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
            Tuple.Create(new Regex(
                @"(?:^|\s+)permainan untuk dikenang(?:\s+sepanjang(?:\s+masa|\s+waktu))?\.?\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
            Tuple.Create(new Regex(
                @"(?:^|\s+)klasik untuk segala (?:usia|zaman)\.?\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
            Tuple.Create(new Regex(
                @"(?:^|\s+)mengingatkan kita (?:mengapa|kenapa) (?:kita\s+)?(?:mencintai|menyukai|memainkan) catur\.?\s*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled), " "),
        };
        private static readonly Regex _blankLineRun = new Regex(@"\n{3,}");

        private static Tuple<Regex, string> New(string pattern, string repl)
        {
            return Tuple.Create(
                new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled),
                repl);
        }

        /// <summary>Apply word substitutions only (use for short fields like
        /// title, social caption, meta description).</summary>
        public static string ScrubInline(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            foreach (var r in _rules)
            {
                s = r.Item1.Replace(s, m => CasePreserve(m.Value, r.Item2));
            }
            return s.Trim();
        }

        /// <summary>Apply word substitutions + body-specific structural
        /// cleanup (numbered/bullet lessons, standalone bold headers).</summary>
        public static string ScrubBody(string body)
        {
            if (string.IsNullOrEmpty(body)) return body;
            body = ScrubInline(body);
            body = _numberedBold.Replace(body, m =>
                m.Groups[1].Value.Trim().TrimEnd('.').TrimEnd(':') + ". ");
            body = _bulletBold.Replace(body, m =>
                m.Groups[1].Value.Trim().TrimEnd('.').TrimEnd(':') + ". ");
            body = _standaloneBold.Replace(body, "");
            // Strip cliched closing patterns flagged by editorial review.
            foreach (var r in _closingCliches)
                body = r.Item1.Replace(body, r.Item2);
            // "a innocuous" -> "an innocuous" (preserve case).
            body = _aVowel.Replace(body,
                m => (m.Groups[1].Value == "A" ? "A" : "a") + "n ");
            body = _blankLineRun.Replace(body, "\n\n");
            return body.Trim() + "\n";
        }

        // Match the casing of the original token on the replacement.
        private static string CasePreserve(string orig, string repl)
        {
            if (string.IsNullOrEmpty(orig) || string.IsNullOrEmpty(repl)) return repl;
            if (orig.ToUpperInvariant() == orig) return repl.ToUpperInvariant();
            if (char.IsUpper(orig[0]))
                return char.ToUpperInvariant(repl[0]) +
                       (repl.Length > 1 ? repl.Substring(1) : "");
            return repl;
        }
    }
}
