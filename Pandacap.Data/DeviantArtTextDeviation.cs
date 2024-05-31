namespace Pandacap.Data
{
    public class DeviantArtTextDeviation : DeviantArtDeviation
    {
        public string? Excerpt { get; set; }

        public override bool RenderAsArticle => !string.IsNullOrEmpty(Title);
    }
}
