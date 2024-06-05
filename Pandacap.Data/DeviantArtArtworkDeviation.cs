﻿namespace Pandacap.Data
{
    public class DeviantArtArtworkDeviation : DeviantArtDeviation, IThumbnail, IImagePost
    {
        public class DeviantArtImage
        {
            public string Url { get; set; } = "";
            public string ContentType { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public DeviantArtImage Image { get; set; } = new();

        public class DeviantArtThumbnailRendition : IThumbnailRendition
        {
            public string Url { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public List<DeviantArtThumbnailRendition> ThumbnailRenditions { get; set; } = [];

        public string? AltText { get; set; }

        public override bool RenderAsArticle => false;

        IEnumerable<IThumbnail> IImagePost.Thumbnails => [this];

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;
    }
}
