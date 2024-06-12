﻿namespace Pandacap.Data
{
    public class FeedItem : IPost
    {
        public Guid Id { get; set; }

        public string? FeedTitle { get; set; }

        public string? FeedWebsiteUrl { get; set; }

        public string? FeedIconUrl { get; set; }

        public string? Title { get; set; }

        public string? Url { get; set; }

        public string? HtmlDescription { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        private record Image(string Url) : IThumbnail, IThumbnailRendition
        {
            IEnumerable<IThumbnailRendition> IThumbnail.Renditions => [this];
            string? IThumbnail.AltText => null;

            int IThumbnailRendition.Width => 0;
            int IThumbnailRendition.Height => 0;
        }

        public IEnumerable<IThumbnail> Thumbnails
        {
            get
            {
                string work = HtmlDescription ?? "";
                while (true)
                {
                    int index = work.IndexOf("src=", StringComparison.InvariantCultureIgnoreCase);
                    if (index == -1)
                        break;

                    work = work[(index + 4)..];

                    char stringDelimeter = work.DefaultIfEmpty('\0').First();
                    if (stringDelimeter != '"' && stringDelimeter != '\'')
                        continue;

                    work = work[1..];

                    int endIndex = work.IndexOf(stringDelimeter);
                    string url = work[..endIndex];
                    work = work[endIndex..];

                    yield return new Image(url);
                }
            }
        }

        string IPost.Id => $"{Id}";

        string? IPost.Username => FeedTitle;

        string? IPost.Usericon => FeedIconUrl;

        string? IPost.DisplayTitle => Title;

        DateTimeOffset IPost.Timestamp => Timestamp;

        string? IPost.LinkUrl => Url;
    }
}