using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChessArticle
{
    /// <summary>One move in a parsed PGN game.</summary>
    public class PgnMove
    {
        public int MoveNumber { get; set; }   // full-move number (1, 1, 2, 2, ...)
        public bool IsWhite { get; set; }
        public string San { get; set; }       // e.g. "Nf3", "Rxd4", "O-O"
        public bool HasEval { get; set; }
        public double Eval { get; set; }      // pawns; mate scores clamped to +/- 100
        public string Clock { get; set; }     // e.g. "0:01:23" if present

        public PgnMove()
        {
            San = "";
            Clock = "";
        }

        /// <summary>"24.Bc3" or "24...Rxd4" - how the move reads in text.</summary>
        public string Label
        {
            get { return MoveNumber + (IsWhite ? "." : "...") + San; }
        }
    }

    /// <summary>A parsed PGN game: headers + moves.</summary>
    public class PgnGame
    {
        public Dictionary<string, string> Headers { get; private set; }
        public List<PgnMove> Moves { get; private set; }
        public string Result { get; set; }

        // Optional roster enrichment. A caller may populate these through
        // PgnParser.RosterResolver to turn a raw handle into a real name,
        // title, team, and rating. Empty strings mean "no roster data, fall
        // back to the raw PGN headers" - the prompt builder treats them as
        // optional throughout.
        public string WhiteFullName { get; set; }
        public string BlackFullName { get; set; }
        public string WhiteTitle    { get; set; }   // FM / IM / GM / NM
        public string BlackTitle    { get; set; }
        public string WhiteTeam     { get; set; }   // team code, e.g. "ACME"
        public string BlackTeam     { get; set; }
        public string WhiteTeamName { get; set; }   // team name, e.g. "Acme Club"
        public string BlackTeamName { get; set; }
        public int    WhiteRosterRating { get; set; }   // rating supplied by the roster
        public int    BlackRosterRating { get; set; }
        public string TournamentName { get; set; }

        public PgnGame()
        {
            Headers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            Moves = new List<PgnMove>();
            Result = "";
            WhiteFullName = ""; BlackFullName = "";
            WhiteTitle = "";    BlackTitle = "";
            WhiteTeam = "";     BlackTeam = "";
            WhiteTeamName = ""; BlackTeamName = "";
            TournamentName = "";
        }

        /// <summary>
        /// Parse the numeric chess.com link id out of the PGN [Link] header.
        /// PGNs from chess.com look like:
        ///   [Link "https://www.chess.com/game/live/122221771066"]
        /// Returns 0 when there is no [Link] tag or it isn't a chess.com URL.
        /// </summary>
        public long ChesscomLinkID
        {
            get
            {
                string link = Header("Link");
                if (string.IsNullOrEmpty(link)) return 0;
                Match m = Regex.Match(link, @"chess\.com/game/(?:live/)?(\d+)", RegexOptions.IgnoreCase);
                if (!m.Success) return 0;
                long n;
                return long.TryParse(m.Groups[1].Value, out n) ? n : 0;
            }
        }

        /// <summary>True when roster data was supplied for this game.</summary>
        public bool HasRoster
        {
            get { return !string.IsNullOrEmpty(WhiteTeam) || !string.IsNullOrEmpty(BlackTeam); }
        }

        public string Header(string key)
        {
            string v;
            return Headers.TryGetValue(key, out v) ? v : "";
        }

        public string White { get { return Header("White"); } }
        public string Black { get { return Header("Black"); } }
        public string Event { get { return Header("Event"); } }
        public string Round { get { return Header("Round"); } }
        public string Date { get { return Header("Date"); } }
        public string WhiteElo { get { return Header("WhiteElo"); } }
        public string BlackElo { get { return Header("BlackElo"); } }
        public string WhiteUrl { get { return Header("WhiteUrl"); } }
        public string BlackUrl { get { return Header("BlackUrl"); } }
        public string Site { get { return Header("Site"); } }

        /// <summary>True if any move carries a [%eval] annotation.</summary>
        public bool HasEvals
        {
            get
            {
                foreach (PgnMove m in Moves) if (m.HasEval) return true;
                return false;
            }
        }
    }
}
