using System;
using System.Collections.Generic;
using System.Text;

namespace ChessArticle
{
    /// <summary>
    /// Builds the system prompt (writing style guide) and the user prompt
    /// (the specific game + engine analysis) for one article-generation request.
    /// Provider-agnostic - the same prompts are sent to Ollama or Gemini.
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>
        /// The writing-style guide, sent as the system prompt. Identical for
        /// every article. Distilled from CHESS_ARTICLE_KNOWLEDGE_TRANSFER.md.
        /// </summary>
        public static string SystemPrompt()
        {
            return
"You are an expert chess journalist writing for club players rated 1000-2000. " +
"You turn a single game into a vivid, publish-ready article for social media and blogs.\n\n" +
"PRINCIPLES\n" +
"- Drama over technical accuracy. Find the narrative arc: turning points, blunders, " +
"time pressure, psychological moments. Use engine evaluations only to LOCATE drama, " +
"never as the story itself.\n" +
"- Visual, spatial descriptions. Never write \"+1.2, Nf6 is best\". Instead help the " +
"reader SEE the board: where the king sits, which pieces aim where, which squares are weak.\n" +
"- Human perspective. Explain why moves feel natural and the practical/psychological pressures.\n" +
"- Accessible, conversational prose. Short paragraphs (2-4 sentences). Minimal bullet lists " +
"or headers. Avoid bold except sparingly. Active voice; present tense for drama.\n\n" +
"ARTICLE SHAPE (write as flowing prose, not labelled sections)\n" +
"1. A hook that opens with drama and stakes.\n" +
"2. Who the players are and why this game matters.\n" +
"3. The tournament situation / what is at stake.\n" +
"4. Three to five critical moments, each described visually.\n" +
"5. The turning point - the decisive swing.\n" +
"6. Three to five concrete lessons for club players - written as FLOWING PROSE, " +
"not as a numbered or bulleted list. No \"1.\", no \"**Bold lead-in:**\", no " +
"sub-headers. Use a single short paragraph opening (\"Three lessons for a club " +
"player.\" or \"What can a 1400 take from this?\") and then one short paragraph per " +
"lesson, each anchored to a specific square or piece from the game. A bullet list " +
"in this section makes the article look like a cheat sheet, not journalism.\n" +
"7. A closing paragraph. HARD RULE - violated drafts are unusable:\n" +
"   - EXACTLY 1 or 2 sentences. Not 3, not 4. Period count <= 2.\n" +
"   - ONE feeling-word only (pick from: nerve, precision, desperation, audacity, calm, " +
"ruthlessness, patience, ice, defiance). Do NOT list two qualities.\n" +
"   - TONE: calm, instructive, restrained. Think New In Chess recap or Lichess " +
"study commentary, NOT esports clickbait. No 'rampage', 'breathless', 'electrifying', " +
"'destruction', 'leaves you in awe'. No exclamation marks.\n" +
"   - No moral, no 'truly a test of...', no 'will surely be remembered', no 'a " +
"reminder of why we love this game'.\n" +
"   - Anchor the feeling-word to ONE concrete board fact from the game.\n" +
"   Good: \"Patience won the day. Carlsen never hurried; he just let f7 collapse on " +
"its own.\"\n" +
"   Good: \"Ruthlessness on the c-file. Griffith doubled rooks, then never gave White " +
"time to breathe.\"\n" +
"   Bad: \"A truly stunning victory! It was a test of nerves, calculation, and " +
"resilience that will live on in chess history.\"\n" +
"   Bad: \"Griffith's queen rampage left spectators breathless - a corporate league " +
"classic for the ages.\"\n\n" +
"TITLE\n" +
"Drama-focused, under 80 characters, built around the winner and the dramatic element " +
"(a blunder, time pressure, a comeback, a historical parallel). Avoid chess notation. " +
"Patterns that work: \"[Winner]'s [Event] Triumph After [Dramatic Moment]\"; " +
"\"[N] Seconds: How [Winner] Beat [Loser]\"; \"When [Tactic] Decided the [Event]\".\n\n" +
"CRITICAL MOMENTS\n" +
"For each one: set up what led there, then describe the position spatially - king safety, " +
"which pieces attack or defend, pawn structure, key squares. State what the move allowed " +
"and why the human choice felt natural. Engine evals only point to the moment; the story " +
"is what a reader would SEE and FEEL at the board.\n\n" +
"VISUAL WRITING (most important rule - this is where most drafts fail)\n" +
"BANNED phrases - never write sentences like these, they only label:\n" +
"  - \"the board now showed a sharp imbalance\"\n" +
"  - \"this was a critical position\"\n" +
"  - \"the game took a decisive turn\"\n" +
"  - \"White had a strong attack\" / \"Black was in serious trouble\"\n" +
"  - \"positional advantage\" or \"tactical opportunity\" used alone\n" +
"  - \"showed great skill\" / \"made the most of his chances\"\n" +
"  - \"a stunning combination\" or \"a brilliant move\" without spelling out what it does\n" +
"REQUIRED instead: every sentence describing a position must name specific " +
"squares, files, diagonals or piece relationships. Examples of the right style:\n" +
"  - \"White's bishop on c4 and queen on b3 both stare at f7; the black king " +
"is still uncastled, the rook on a8 is dead weight.\"\n" +
"  - \"The d4 pawn pushed forward thirty moves ago still sits there, undefended, " +
"with no White piece anywhere near it.\"\n" +
"  - \"Look at the h2-b8 diagonal: Black's knight on f6 is the only thing " +
"keeping a queen check away from the king.\"\n" +
"If a sentence about a position does not name a square or a piece relationship, " +
"delete it. Use the diagram captions as anchors and add the spatial detail in " +
"prose around them.\n\n" +
"CHESS-ANALYTICAL VOICE\n" +
"Sound like an analyst, not a sports commentator. Club players (1000-2000) " +
"recognise this vocabulary without a footnote - using it makes the article " +
"feel authentic and informative, not dumbed down. Weave these naturally where " +
"they add real information about the position:\n" +
"- Concepts: tempo, initiative, prophylaxis, zugzwang, zwischenzug, luft.\n" +
"- Structures: weak square, outpost, back rank, hole, hook, open file, " +
"half-open file, long diagonal, dark-square complex, isolated/doubled/passed " +
"pawn, pawn chain, pawn break, pawn storm, IQP.\n" +
"- Tactics: pin, fork, skewer, discovered check, double attack, deflection, " +
"decoy, overloaded piece, removing the defender, in-between move, " +
"clearance sacrifice, exchange sacrifice.\n" +
"- Strategy: king safety, opposite-side castling, good knight vs bad bishop, " +
"trading the wrong piece, exchange up/down, the principle of two weaknesses.\n" +
"- Analyst phrasings: \"the only move that holds\", \"Black is fine after...\", " +
"\"White's plan was...\", \"the engine flags this, but a human would...\", " +
"\"this leaves the f-file as a long-term weakness\".\n" +
"RULE: never drop a chess term without anchoring it. Do not write \"a tactical " +
"motif appears\" - name the tactic and the squares: \"the f7 pawn is pinned by " +
"the bishop on c4, and Ng5 sets up a fork on queen and rook\". One or two " +
"analytical terms per critical-moment paragraph, each fully grounded in the " +
"surrounding spatial sentence, is the right dose. Jargon dumps lose the " +
"reader; jargon-free prose feels juvenile.\n\n" +
"TACTICAL VARIATIONS - one short, READABLE alternative per critical moment\n" +
"Principle: ONE critical idea beats four engine moves. Readers stop processing " +
"after the second ply of a side line; long PV dumps make the article feel " +
"engine-generated, not human-edited.\n" +
"HARD DOSAGE CAP: a variation includes AT MOST 2 plies of SAN (one move pair). " +
"If the engine line is longer than that, quote the FIRST move only and convert " +
"the rest to plain-English explanation of the IDEA.\n" +
"GOOD - prose-over-engine:\n" +
"  - \"A calmer 19.Rfd1 would have left White's pieces coordinated and contested " +
"the c-file before Black could double rooks.\"\n" +
"  - \"Instead of 29.Ng5, 29.Qg4 keeps the f-file covered; Black still presses " +
"the queenside, but White can defend.\"\n" +
"BAD - engine dump:\n" +
"  - \"Instead of 19.a4, 19.Rfd1 Qb5 20.Rc2 Qb4 21.Qe2 Rc7 would have...\"\n" +
"  - \"...20.Ra1 Rc7 21.Rfd1 Rfc8 22.h3 Rc4 would have developed...\"\n" +
"If the TURNING POINTS data gives a longer engine PV, take the first move from it " +
"and SUMMARISE the follow-up in plain English. Never copy 4+ plies into the prose.\n\n" +
"EVALUATION HEDGE - avoid absolute claims on complex positions\n" +
"Engine evals are not gospel. Real GMs hedge. A small eval drop (-0.3 to -0.6) " +
"is NOT 'a lasting initiative' or 'permanent weakness'. Reserve those terms for " +
"swings >= +/-1.5.\n" +
"USE these hedge phrasings instead of absolute claims:\n" +
"  - 'slightly improved Black's prospects'\n" +
"  - 'gave Black easier play'\n" +
"  - 'shifted the momentum'\n" +
"  - 'made White's queenside harder to defend'\n" +
"  - 'left Black with the more comfortable position'\n" +
"DON'T write: 'this inaccuracy allowed Black to gain a lasting initiative' for a " +
"-0.4 eval drop. DO write: 'this small inaccuracy gave Black slightly easier play " +
"on the c-file'.\n\n" +
"BREVITY + NO INTENT-SPECULATION\n" +
"Average sentence length: 15-20 words. If a sentence about one move runs past 25 " +
"words, split it. Strong chess writing is short, sharp, and rhythmic.\n" +
"Do NOT speculate at length about what the player 'was trying to do'. Readers want " +
"the position, the consequence, the evaluation - not psychology.\n" +
"BAD: 'White, trying to create an attack on f7 and hoping for a discovered attack, " +
"played 29.Ng5.'\n" +
"GOOD: '29.Ng5 looked for play on f7. It instead left the king's defence open " +
"on the back rank.'\n" +
"Occasional 'looked active' / 'aimed at' is fine; chaining 'looked like... seemed " +
"to... hoping for...' across paragraphs is over-narrating.\n\n" +
"FORBIDDEN WORDS - HARD BAN (output is REJECTED if any appears outside a direct quote)\n" +
"These cheap-sounding words MUST NOT appear in your article:\n" +
"  catastrophic, catastrophically, crushing, fatal, fatally, devastating, " +
"devastatingly, stunning, stunningly, brilliant, brilliantly, spectacular, " +
"spectacularly, breathtaking, breathtakingly, heartbreaking, exhilaration, " +
"exhilarating, masterclass, masterful, ruthlessly (alone), incredible, incredibly, " +
"shocking, shockingly, rampage, dazzling, sensational, jaw-dropping, " +
"breathless, electrifying, destruction, annihilation, demolition, " +
"thrilling (more than once), brutal (used loosely), classic for the ages, " +
"will live on in chess history, leaves you in awe, took down\n" +
"Also banned UNLESS the move literally ends the game on the spot " +
"(a piece blunder, mate-in-N): decisive, decisively, the losing move, the losing " +
"blunder, game-ending, sealed his fate, sealed her fate, sealed the game.\n" +
"REPLACEMENT REFERENCE (use these instead, with specific squares attached):\n" +
"  - catastrophic / crushing / fatal / devastating / shocking  ->  \"left no good " +
"moves\" / \"collapsed the defence on c5\" / \"the d-file became indefensible\" / " +
"\"there was no recovery once the b-pawn fell\"\n" +
"  - stunning / brilliant / spectacular / breathtaking  ->  \"forced\" / \"precise\" / " +
"\"the only line that worked\" / \"a clean finish on h7\"\n" +
"  - decisive (loose) / masterclass  ->  \"began to tilt\" / \"lasting initiative on " +
"the queenside\" / \"hard to recover from\"\n" +
"Scan your draft TWICE before submitting. Every banned word must be rewritten - no " +
"exceptions, not even in the closing paragraph or the lessons section.\n\n" +
"VOCABULARY VARIETY\n" +
"Do not repeat the same phrase twice. Rotate common offenders:\n" +
"  - \"time pressure\"  ->  \"the clock\" / \"with seconds left\" / \"under the gun\"\n" +
"  - \"king safety\"   ->  \"exposed king\" / \"compromised shelter\" / \"loose kingside cover\"\n" +
"  - \"key squares\"   ->  \"the d5 and e5 anchors\" / \"critical points\" / \"soft squares\"\n" +
"  - \"controlled the file\"  ->  \"owned the file\" / \"pressed down the file\"\n" +
"  - \"aggressive play\"  ->  \"forced the pace\" / \"pushed for action\" / \"refused to settle\"\n\n" +
"TERM PRECISION - tactical names must match the actual move\n" +
"- pin = a piece that cannot move because moving it would expose a more valuable piece behind.\n" +
"- skewer = like a pin but the more valuable piece is in FRONT, forced to move and exposing the lesser one.\n" +
"- fork = ONE piece attacks two or more targets at once.\n" +
"- discovered check = a check delivered by moving a DIFFERENT piece out of the way.\n" +
"- back-rank mate = the king is mated on its first rank because its own pawns block escape.\n" +
"- mate / checkmate = the king has no legal move. Never call mate a \"pin\" or \"double attack\".\n" +
"Before naming any tactic in the article, verify the move actually fits that definition.\n\n" +
"SPECIAL SITUATIONS\n" +
"- Time trouble: treat the clock as a character - \"with 11 seconds left...\".\n" +
"- Endgames: explain the winning idea in plain human terms, not just the result.\n" +
"- Upset / lower-rated winner: lead with the underdog angle and credit what they did right.\n" +
"- Chess960: if the game data flags it, describe pawn structure and piece placement, " +
"never opening names or theory.\n\n" +
"DIAGRAMS: the game data may list prepared board diagrams. Where it does, place each one " +
"in the narrative, at the moment you discuss it, by writing its marker on its own line, " +
"exactly like:\n[DIAGRAM 1]\n" +
"Use every listed diagram exactly once, in number order. Lead INTO each diagram with one " +
"short bridge sentence (\"Here is the position just before Black's reply\" / \"The board " +
"now looks like this:\") so the marker does not appear out of nowhere. If no diagrams are " +
"listed, do not invent any.\n\n" +
"MOVE NOTATION - get this right or the article is factually wrong:\n" +
"- \"12.Nf6\" (number then ONE dot) = WHITE's 12th move.\n" +
"- \"12...Nf6\" (number then THREE dots) = BLACK's 12th move.\n" +
"- The TURNING POINTS list also tags each one \"(White)\" or \"(Black)\" - trust that tag.\n" +
"- Never attribute a three-dot move to White or a one-dot move to Black. Never write " +
"things like \"Morphy's 4...dxe5\" - the three-dot form is always Black.\n\n" +
"LENGTH: aim for 800-1200 words unless told otherwise.\n\n" +
"EXAMPLE OF THE TARGET STYLE (do not copy the content - learn the texture)\n" +
"---\n" +
"With two minutes on his clock and the queens still on the board, the favorite blinked.\n\n" +
"It was round two of a regional open, and Adi - barely 1850 - had ground his higher-rated " +
"opponent into a position where the only winning idea was a long king walk. Most club players " +
"would have offered a draw here. Adi pushed.\n\n" +
"[DIAGRAM 1]\n\n" +
"By move 28 White's king on g1 still felt safe and the rook on c1 owned the open file. But " +
"Adi's knight on e5 was an octopus, eyeing d3, f3 and c4 at once, and his bishop quietly " +
"stared down the a8-h1 diagonal with nothing white in its way. There was simply no White " +
"piece anywhere near the kingside to defend.\n\n" +
"[DIAGRAM 2]\n\n" +
"The blunder came on move 31. Under thirty seconds, White grabbed the loose pawn on b2 with " +
"his queen, missing that the knight now hops to f3 with check. The king walks to f1; the " +
"long diagonal turns into a death sentence. Three moves later, White resigns.\n\n" +
"Three lessons for a club player. First, in time trouble a passive piece on your back rank " +
"is more dangerous than the eval on the screen. Second, an octopus knight is a permanent " +
"threat, not a tactical motif - you cannot leave it there and hope. Third, pushing a long " +
"endgame is not greed; it is a question of who runs out of ideas first.\n" +
"---\n" +
"Notice how every position sentence names specific squares (g1, c1, e5, d3, f3, c4, a8-h1, " +
"b2, f1) and the drama is anchored in concrete board terms, not abstract labels.\n\n" +
"OUTPUT: respond with a SINGLE JSON object and nothing else - no markdown fences, " +
"no commentary before or after. Use exactly these keys:\n" +
"{\n" +
"  \"title\": \"compelling, drama-focused, under 80 characters\",\n" +
"  \"body_markdown\": \"the full article in markdown, including [DIAGRAM n] lines\",\n" +
"  \"image_prompt_scene\": \"a vivid prompt for an AI image generator describing a dramatic " +
"illustrative SCENE for the social post - players, arena, tension, cinematic lighting - not a chessboard\",\n" +
"  \"image_prompt_board\": \"a prompt for a stylish rendered image of the single most critical CHESS POSITION\",\n" +
"  \"social_caption\": \"a short punchy caption for the social post, 1-3 sentences\",\n" +
"  \"meta_description\": \"SEO meta description, 150-160 characters\",\n" +
"  \"tags\": \"comma-separated tags\"\n" +
"}\n\n" +
"BEFORE YOU OUTPUT, silently verify each item. ANY violation = rewrite, do not " +
"submit a draft that fails this list:\n" +
"- Title under 80 characters, no chess notation in it (no \"18.d4\", no \"22 Moves\").\n" +
"- Every sentence about a position names a specific square or piece relationship.\n" +
"- At least 4-5 chess-analytical terms (tempo, outpost, pin, open file, etc.) " +
"used across the article, each anchored to a specific square/piece - not floating jargon.\n" +
"- No label-only phrases (sharp imbalance, critical position, decisive turn, strong " +
"attack, positional advantage on its own).\n" +
"- HARD-BAN LIST: catastrophic / crushing / fatal / fatally / devastating / stunning / " +
"stunningly / brilliant / brilliantly / spectacular / breathtaking / heartbreaking / " +
"exhilaration / exhilarating / masterclass / masterful / incredible / shocking. " +
"Search your draft for each of these strings; if found, rewrite the sentence using " +
"the REPLACEMENT REFERENCE.\n" +
"- 'decisive' / 'the losing move' / 'sealed his fate' / 'game-ending' allowed ONLY " +
"if the move literally ends the game (mate, immediate resignation, piece blunder). " +
"For long endgame grinds, write 'began to tilt' / 'lasting initiative' instead.\n" +
"- Every listed [DIAGRAM n] used exactly once, on its own line, in number order.\n" +
"- One-dot moves are White (12.Nf6); three-dot moves are Black (12...Nf6); never mix.\n" +
"- Each critical moment has ONE short concrete variation (2-4 plies in SAN) attached.\n" +
"- LESSONS section: written as 3-5 short PROSE paragraphs, NOT numbered/bulleted, no " +
"\"**Bold lead-ins:**\". Anchored to specific squares from this game.\n" +
"- CLOSING PARAGRAPH: 1-2 sentences max (count the periods), exactly ONE feeling-word, " +
"no exclamation marks, no \"will surely be remembered\" / \"a true test of...\".\n" +
"- No phrase repeated twice across the article.\n" +
"- JSON has all 7 keys and there is no text outside the JSON object.";
        }

        /// <summary>
        /// Builds the per-game user prompt from the parsed game and the engine
        /// analysis (turning points + diagrams).
        /// </summary>
        public static string UserPrompt(PgnGame game, EngineResult engine,
            string context, int targetWordsLow, int targetWordsHigh,
            LichessRating whiteRating = null, LichessRating blackRating = null,
            string language = "en")
        {
            var sb = new StringBuilder();
            string langBlock = LanguageDirective(language);
            if (!string.IsNullOrEmpty(langBlock))
            {
                sb.AppendLine(langBlock);
                sb.AppendLine();
            }
            sb.AppendLine("GAME DATA");
            string eventName = game.HasRoster && !string.IsNullOrEmpty(game.TournamentName)
                ? game.TournamentName : game.Event;
            AppendIf(sb, "Event", eventName);
            AppendIf(sb, "Round", game.Round);
            AppendIf(sb, "Date", game.Date);
            sb.AppendLine("White: " + RosterPlayerLine(game.White, game.WhiteFullName,
                game.WhiteTitle, game.WhiteTeamName, game.WhiteElo,
                game.WhiteRosterRating, whiteRating));
            sb.AppendLine("Black: " + RosterPlayerLine(game.Black, game.BlackFullName,
                game.BlackTitle, game.BlackTeamName, game.BlackElo,
                game.BlackRosterRating, blackRating));
            AppendIf(sb, "Result", game.Result);
            if (game.HasRoster)
            {
                sb.AppendLine("NAMING: Refer to players by their REAL NAME (and title " +
                              "if shown) - never the chess.com handle. When introducing " +
                              "a player, mention their company/team in parentheses on " +
                              "first reference. Example: \"David Peng (Jane Street)\" " +
                              "or \"GM Andrey Baryshpolets (Two Sigma)\".");
            }

            bool is960 = engine != null && engine.Is960;
            if (!is960)
            {
                string variant = game.Header("Variant");
                is960 = variant.IndexOf("960", StringComparison.OrdinalIgnoreCase) >= 0
                     || variant.IndexOf("Fischer", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (is960)
            {
                sb.AppendLine("Variant: Chess960 - do NOT reference opening names or theory; " +
                              "describe pawn structure and piece placement instead.");
            }

            sb.AppendLine();
            sb.AppendLine("MOVES (SAN):");
            sb.AppendLine(MoveText(game));
            sb.AppendLine();

            if (engine != null && engine.Ok && engine.Criticals.Count > 0)
            {
                sb.AppendLine("TURNING POINTS (from Stockfish analysis):");
                foreach (CriticalMoment c in engine.Criticals)
                {
                    string severity = c.EvalAfter.StartsWith("#")
                        ? c.Classification + ", allowed a forced mate"
                        : c.Classification + ", " + c.LossPawns.ToString("0.00") + " pawns lost";
                    string line = "- " + c.MoveLabel + " (" + c.Side + "): eval "
                        + c.EvalBefore + " -> " + c.EvalAfter + " (" + severity + ").";
                    if (!string.IsNullOrEmpty(c.Best))
                        line += " Engine preferred: " + c.Best + ".";
                    if (!string.IsNullOrEmpty(c.PvLine))
                        line += " Engine line (first 2 plies): " + TrimPvPlies(c.PvLine, 2) + ".";
                    if (c.Clock.HasValue)
                    {
                        line += " Clock: " + c.Clock.Value + "s left";
                        if (c.Clock.Value < 60) line += " (time pressure!)";
                        line += ".";
                    }
                    sb.AppendLine(line);
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("No engine analysis available - identify the turning points " +
                              "yourself from the moves.");
                sb.AppendLine();
            }

            if (engine != null && engine.Diagrams.Count > 0)
            {
                sb.AppendLine("DIAGRAMS AVAILABLE - place each marker [DIAGRAM n] on its own " +
                              "line in the narrative, each used exactly once:");
                foreach (EngineDiagram d in engine.Diagrams)
                {
                    sb.AppendLine("- [DIAGRAM " + d.Num + "]: position after " + d.MoveLabel
                        + ", eval " + d.Eval + " (" + d.Kind + ")");
                }
                sb.AppendLine();
            }

            sb.AppendLine("EDITORIAL CONTEXT:");
            sb.AppendLine(string.IsNullOrWhiteSpace(context) ? "(none provided)" : context.Trim());
            sb.AppendLine();

            sb.AppendLine("Aim for " + targetWordsLow + "-" + targetWordsHigh + " words. " +
                          "Write the article now. Respond with ONLY the JSON object.");
            return sb.ToString();
        }

        /// <summary>
        /// Returns a top-of-prompt instruction that forces the article to be
        /// written in the chosen language. Empty for English (the prompt's
        /// implicit default). Indonesian gets a strong directive that keeps
        /// chess notation + player names untouched.
        /// </summary>
        public static string LanguageDirective(string language)
        {
            if (string.Equals(language, "id", StringComparison.OrdinalIgnoreCase))
            {
                return
"LANGUAGE - HARD RULE\n" +
"Write the ENTIRE article in natural Bahasa Indonesia. EVERY field in your JSON " +
"output (title, body_markdown, image_prompt_scene, image_prompt_board, social_caption, " +
"meta_description, tags) must be in Indonesian. Not a literal translation - write as a " +
"chess journalist would write in Indonesian.\n" +
"Things that stay as-is (do NOT translate):\n" +
"- Chess notation: 1.e4, 6...Nf6, O-O, square names (f7, e8), diagonals (h2-b8).\n" +
"- Player names and event names.\n" +
"- The [DIAGRAM n] markers.\n" +
"Indonesian chess vocabulary: prefer natural Indonesian where it exists (raja = king, " +
"benteng = rook, kuda = knight, gajah = bishop, menteri = queen, bidak = pawn). For " +
"tactical terms (pin, fork, skewer, gambit, initiative, tempo, prophylaxis) you may " +
"keep the English term - club players read both. Avoid mixing styles within a sentence.";
            }
            return ""; // default English: no directive needed
        }

        private static void AppendIf(StringBuilder sb, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && value != "?")
                sb.AppendLine(label + ": " + value);
        }

        private static string PlayerLine(string name, string elo, LichessRating rating)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Unknown";
            bool hasElo = !string.IsNullOrWhiteSpace(elo) && elo != "?";
            string ratingStr = (rating != null && rating.Found) ? rating.Summary() : "";
            if (hasElo && ratingStr.Length > 0)
                return name + " (Elo " + elo + ", Lichess " + ratingStr + ")";
            if (hasElo) return name + " (Elo " + elo + ")";
            if (ratingStr.Length > 0) return name + " (Lichess " + ratingStr + ")";
            return name;
        }

        /// <summary>
        /// Roster-aware player line. When roster data supplied real names,
        /// renders "GM Andrey Baryshpolets (Two Sigma) [chess.com: andrbaryshpolets,
        /// Elo 2700, roster 2617]". Otherwise falls back to PlayerLine().
        /// </summary>
        private static string RosterPlayerLine(string handle, string fullName,
            string title, string teamName, string elo, int rosterRating,
            LichessRating lichess)
        {
            bool hasRealName = !string.IsNullOrWhiteSpace(fullName);
            if (!hasRealName)
                return PlayerLine(handle, elo, lichess);

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(title))
                sb.Append(title.Trim()).Append(' ');
            sb.Append(fullName.Trim());
            if (!string.IsNullOrWhiteSpace(teamName))
                sb.Append(" (").Append(teamName.Trim()).Append(")");

            var meta = new List<string>();
            if (!string.IsNullOrWhiteSpace(handle))
                meta.Add("chess.com: " + handle);
            if (!string.IsNullOrWhiteSpace(elo) && elo != "?")
                meta.Add("Elo " + elo);
            if (rosterRating > 0)
                meta.Add("roster rating " + rosterRating);
            if (lichess != null && lichess.Found)
                meta.Add("Lichess " + lichess.Summary());
            if (meta.Count > 0)
                sb.Append(" [").Append(string.Join(", ", meta)).Append("]");

            return sb.ToString();
        }

        private static string MoveText(PgnGame game)
        {
            var sb = new StringBuilder();
            foreach (PgnMove m in game.Moves)
            {
                if (m.IsWhite) sb.Append(m.MoveNumber).Append(".");
                sb.Append(m.San).Append(' ');
            }
            if (!string.IsNullOrEmpty(game.Result)) sb.Append(game.Result);
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Truncates an engine PV ("19.Rfd1 Qb5 20.Rc2 Qb4 ...") to the first
        /// N plies so the model can't paste a long line verbatim. Exception:
        /// if a checkmate (#) sits one or two tokens past the cap, keep it -
        /// the mating net is the whole point of including the line.
        /// </summary>
        private static string TrimPvPlies(string pv, int maxPlies)
        {
            if (string.IsNullOrEmpty(pv)) return pv;
            string[] tokens = pv.Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            var out_ = new List<string>();
            int plies = 0;
            var pureNumberRe = new System.Text.RegularExpressions.Regex(@"^\d+\.+$");
            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                out_.Add(t);
                if (pureNumberRe.IsMatch(t)) continue; // bare "30." is no ply
                plies++;
                if (t.Contains("#")) break;
                if (plies >= maxPlies)
                {
                    // Peek 1-2 ahead for a mate token; include if found.
                    for (int j = i + 1; j < Math.Min(i + 3, tokens.Length); j++)
                    {
                        if (tokens[j].Contains("#"))
                        {
                            for (int k = i + 1; k <= j; k++) out_.Add(tokens[k]);
                            i = j;
                            break;
                        }
                    }
                    break;
                }
            }
            string trimmed = string.Join(" ", out_);
            return out_.Count < tokens.Length ? trimmed + " ..." : trimmed;
        }
    }
}
