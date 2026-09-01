using System.Collections.Generic;

namespace ChessArticle
{
    /// <summary>
    /// Rating snapshot for one player. Optional context for the prompt — when
    /// no rating source is wired up, the prompt simply omits it.
    /// </summary>
    public class LichessRating
    {
        public string Username { get; set; }
        public bool Found { get; set; }
        public int Classical { get; set; }
        public int Rapid { get; set; }
        public int Blitz { get; set; }
        public int Bullet { get; set; }
        public int ClassicalGames { get; set; }
        public int RapidGames { get; set; }
        public int BlitzGames { get; set; }
        public int BulletGames { get; set; }
        public string Error { get; set; }

        public LichessRating()
        {
            Username = "";
            Error = "";
        }

        /// <summary>"Rapid 1955 / Blitz 1910" — the top two ratings as a one-liner.</summary>
        public string Summary()
        {
            if (!Found) return "";
            var parts = new List<string>();
            if (Rapid > 0) parts.Add("Rapid " + Rapid);
            if (Blitz > 0) parts.Add("Blitz " + Blitz);
            if (Classical > 0) parts.Add("Classical " + Classical);
            if (Bullet > 0) parts.Add("Bullet " + Bullet);
            if (parts.Count > 2) parts.RemoveRange(2, parts.Count - 2);
            return parts.Count > 0 ? string.Join(" / ", parts) : "";
        }
    }

    /// <summary>
    /// One board diagram belonging to an article: an SVG snapshot of a
    /// position, its caption, and where in the game it came from.
    /// </summary>
    public class ArticleDiagram
    {
        public int Num { get; set; }
        public string Caption { get; set; }
        public string Svg { get; set; }

        /// <summary>Which game this diagram belongs to in a multi-game article. 0 for single-game.</summary>
        public int GameIndex { get; set; }

        /// <summary>The half-move this diagram captures, so a viewer can deep-link to it. 0 means unknown.</summary>
        public int Ply { get; set; }

        public ArticleDiagram()
        {
            Caption = "";
            Svg = "";
        }
    }
}
