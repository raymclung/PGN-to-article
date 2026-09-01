namespace ChessArticle
{
    /// <summary>
    /// Outcome of one generation run: either a populated article, or an error.
    /// </summary>
    public class ArticleResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        public string Title { get; set; }
        public string BodyMarkdown { get; set; }
        public string ImagePromptScene { get; set; }
        public string ImagePromptBoard { get; set; }
        public string SocialCaption { get; set; }
        public string MetaDescription { get; set; }
        public string Tags { get; set; }

        public ArticleResult()
        {
            Success = false;
            Error = "";
            Title = "";
            BodyMarkdown = "";
            ImagePromptScene = "";
            ImagePromptBoard = "";
            SocialCaption = "";
            MetaDescription = "";
            Tags = "";
        }
    }
}
