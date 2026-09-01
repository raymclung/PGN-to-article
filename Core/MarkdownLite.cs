using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ChessArticle
{
    /// <summary>
    /// Tiny markdown-to-HTML converter - enough for the article style used here
    /// (prose paragraphs, occasional bold/italic, headings, and [DIAGRAM n]
    /// markers). No NuGet dependency.
    /// </summary>
    public static class MarkdownLite
    {
        /// <summary>Render markdown with no diagrams (markers shown as text).</summary>
        public static string ToHtml(string markdown)
        {
            return ToHtml(markdown, null, null, "id");
        }

        /// <summary>Render markdown with diagrams but no viewer link.</summary>
        public static string ToHtml(string markdown, List<ArticleDiagram> diagrams)
        {
            return ToHtml(markdown, diagrams, null, "id");
        }

        /// <summary>Back-compat overload: defaults to Indonesian chrome,
        /// matching the original hardcoded "Lihat partai" behaviour.</summary>
        public static string ToHtml(string markdown, List<ArticleDiagram> diagrams,
            Func<ArticleDiagram, string> viewerUrl)
        {
            return ToHtml(markdown, diagrams, viewerUrl, "id");
        }

        /// <summary>
        /// Render markdown. A [DIAGRAM n] marker is replaced by the matching
        /// diagram's inline SVG; if no match is found it falls back to a label.
        /// If viewerUrl is non-null, each rendered diagram also gets a
        /// View-game link to the interactive Viewer for the right game.
        /// <paramref name="lang"/> picks the wording ("id" = Indonesian,
        /// otherwise English).
        /// </summary>
        public static string ToHtml(string markdown, List<ArticleDiagram> diagrams,
            Func<ArticleDiagram, string> viewerUrl, string lang)
        {
            if (string.IsNullOrEmpty(markdown)) return "";

            string text = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

            // Robustness: AI models routinely embed a [DIAGRAM n] marker inside
            // a paragraph instead of on its own block, which would silently
            // render as literal text. Force each marker onto its own block.
            text = Regex.Replace(text,
                @"[ \t]*(\[DIAGRAM\s*\d+[^\]]*\])[ \t]*",
                "\n\n$1\n\n",
                RegexOptions.IgnoreCase);

            string[] blocks = Regex.Split(text, "\n[ \t]*\n");
            var sb = new StringBuilder();

            foreach (string rawBlock in blocks)
            {
                string block = rawBlock.Trim();
                if (block.Length == 0) continue;

                // Diagram marker, e.g. [DIAGRAM 2] or [DIAGRAM 2: After 24.Bc3]
                Match diag = Regex.Match(block, @"^\[DIAGRAM\s*(\d+)?[^\]]*\]$", RegexOptions.IgnoreCase);
                if (diag.Success)
                {
                    sb.Append(RenderDiagram(diag.Groups[1].Value, block, diagrams, viewerUrl, lang));
                    continue;
                }

                // Headings
                Match head = Regex.Match(block, @"^(#{1,4})\s+(.*)$");
                if (head.Success)
                {
                    int level = head.Groups[1].Value.Length + 1; // h2..h5
                    if (level > 6) level = 6;
                    sb.Append("<h").Append(level).Append(">")
                      .Append(Inline(head.Groups[2].Value.Trim()))
                      .Append("</h").Append(level).Append(">\n");
                    continue;
                }

                // Normal paragraph - keep single line breaks inside as <br/>
                string para = Inline(block).Replace("\n", "<br/>\n");
                sb.Append("<p>").Append(para).Append("</p>\n");
            }

            return sb.ToString();
        }

        private static string RenderDiagram(string numText, string block,
            List<ArticleDiagram> diagrams, Func<ArticleDiagram, string> viewerUrl,
            string lang)
        {
            int num;
            ArticleDiagram match = null;
            if (diagrams != null && int.TryParse(numText, out num))
            {
                foreach (ArticleDiagram d in diagrams)
                {
                    if (d.Num == num) { match = d; break; }
                }
            }

            if (match != null && !string.IsNullOrEmpty(match.Svg))
            {
                // Strip the trailing "(eval ...)" that older articles still
                // carry in their stored caption.
                string caption = Regex.Replace(match.Caption ?? "",
                    @"\s*\(eval [^)]+\)\s*$", "");

                // Optional inline "Lihat partai" link sitting right beneath
                // the board, so the reader can jump straight into the Viewer
                // for that game without scrolling to the foot of the article.
                string link = "";
                if (viewerUrl != null)
                {
                    string url = viewerUrl(match);
                    if (!string.IsNullOrEmpty(url))
                    {
                        link = "<p class=\"diagram-viewer-link\"><a href=\""
                            + System.Net.WebUtility.HtmlEncode(url)
                            + "\">&#9654; "
                            + System.Net.WebUtility.HtmlEncode(UiText.ViewGameLink(lang))
                            + "</a></p>";
                    }
                }

                // The link sits OUTSIDE the figure so a flex parent can lay it
                // beside the board (and wrap below on a phone screen).
                return "<div class=\"diagram-row\">" +
                       "<figure class=\"diagram\">" + match.Svg +
                       "<figcaption>" + System.Net.WebUtility.HtmlEncode(caption) + "</figcaption>" +
                       "</figure>" +
                       link +
                       "</div>\n";
            }

            // Fallback: no diagram data - show the marker text.
            string inner = block.Trim('[', ']').Trim();
            return "<div class=\"diagram-marker\">&#9823; " +
                   System.Net.WebUtility.HtmlEncode(inner) + "</div>\n";
        }

        private static string Inline(string raw)
        {
            string s = System.Net.WebUtility.HtmlEncode(raw);
            // **bold**
            s = Regex.Replace(s, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            // *italic*
            s = Regex.Replace(s, @"(?<!\*)\*(?!\s)(.+?)(?<!\s)\*(?!\*)", "<em>$1</em>");
            return s;
        }
    }
}
