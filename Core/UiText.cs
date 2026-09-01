using System;

namespace ChessArticle
{
    /// <summary>
    /// Tiny lookup for UI chrome strings that surround an article (the
    /// "View game" / "Lihat partai" link, section headings, etc.). Driven by
    /// <c>Article.Language</c> ("id" or "en") so an English article stops
    /// showing Indonesian buttons.
    ///
    /// Article prose itself is written by the AI in the chosen language, so
    /// it does NOT pass through this helper. This is just for the .aspx /
    /// rendering layer.
    /// </summary>
    public static class UiText
    {
        /// <summary>Normalise the article's Language tag. Anything that
        /// isn't a clean "en" falls back to "id" - matches the old default
        /// before the Language field existed.</summary>
        private static string Norm(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return "id";
            return lang.Trim().ToLowerInvariant() == "en" ? "en" : "id";
        }

        /// <summary>Short label, e.g. for &lt;title&gt; tags and section
        /// headings: "View game" / "Lihat partai".</summary>
        public static string ViewGameTitle(string lang)
        {
            return Norm(lang) == "en" ? "View game" : "Lihat partai";
        }

        /// <summary>Long link label sitting under each diagram and at the
        /// foot of single-game articles:
        /// "View game (interactive board + eval bar)" /
        /// "Lihat partai (papan interaktif + eval bar)".</summary>
        public static string ViewGameLink(string lang)
        {
            return Norm(lang) == "en"
                ? "View game (interactive board + eval bar)"
                : "Lihat partai (papan interaktif + eval bar)";
        }

        /// <summary>Body shown on Viewer.aspx when an article exists but has
        /// no per-ply data (older articles before the Viewer feature). Plain
        /// text - the caller wraps it in &lt;p&gt; / encodes as needed.</summary>
        public static string ViewerNoData(string lang)
        {
            return Norm(lang) == "en"
                ? "No board data for this article - older articles (before this feature) did not store per-move positions. Regenerate the article to enable it."
                : "Tidak ada data papan untuk artikel ini - artikel lama (sebelum fitur ini) tidak menyimpan posisi per langkah. Generate ulang artikelnya untuk mengaktifkannya.";
        }
    }
}
