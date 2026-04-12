using CommonMark;
using Pandacap.ActivityPub.Models.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.PlatformLinks.Interfaces;
using Pandacap.Text;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    public class Post : IPost, IActivityPubPost, IActivityPubAddressing, IPlatformLinkPostSource
    {
        public Guid Id { get; set; }

        public enum PostType
        {
            StatusUpdate = 0,
            JournalEntry = 1,
            Artwork = 2,
            Scraps = 3,
            Link = 4
        };

        public PostType Type { get; set; } = PostType.StatusUpdate;

        public string? Title { get; set; }

        public string? Body { get; set; }

        public class Image
        {
            public class BlobRef
            {
                public Guid Id { get; set; }

                public string ContentType { get; set; } = "application/octet-stream";

                [NotMapped]
                public bool IsVector => ContentType == "image/svg+xml";

                [NotMapped]
                public bool IsRaster => !IsVector;
            }

            public List<BlobRef> Renditions { get; set; } = [];

            public string? AltText { get; set; } = null;

            public class ImageFocalPoint
            {
                public decimal Horizontal { get; set; }

                public decimal Vertical { get; set; }
            }

            public ImageFocalPoint? FocalPoint { get; set; }

            [NotMapped]
            public BlobRef Primary => Renditions[0];

            [NotMapped]
            public BlobRef Raster =>
                Renditions.FirstOrDefault(rendition => rendition.IsRaster)
                ?? Renditions.First();

            [NotMapped]
            public BlobRef PrimaryThumbnail =>
                Renditions.FirstOrDefault(rendition => rendition.IsVector)
                ?? Renditions.Last();
        }

        public List<Image> Images { get; set; } = [];

        public class Link : IActivityPubLink
        {
            public string Url { get; set; } = "";

            public string? SiteName { get; set; }

            public string? Title { get; set; }

            public string? Image { get; set; }

            public string? Description { get; set; }

            string IActivityPubLink.Href => Url;

            string IActivityPubLink.MediaType => "text/html";
        }

        public List<Link> Links { get; set; } = [];

        public List<string> Tags { get; set; } = [];

        public DateTimeOffset PublishedTime { get; set; }

        public string? BlueskyDID { get; set; }

        public string? BlueskyRecordKey { get; set; }

        public Guid? DeviantArtId { get; set; }

        public string? DeviantArtUrl { get; set; }

        public int? FurAffinitySubmissionId { get; set; }

        public int? FurAffinityJournalId { get; set; }

        public int? WeasylSubmitId { get; set; }

        public int? WeasylJournalId { get; set; }

        [NotMapped]
        public bool IsTextPost =>
            Type switch
            {
                PostType.JournalEntry or PostType.StatusUpdate => true,
                _ => false
            };

        [NotMapped]
        public string Html => CommonMarkConverter.Convert(Body ?? "");

        [NotMapped]
        public IEnumerable<Image.BlobRef> Blobs =>
            Images
            .SelectMany(i => i.Renditions);

        [NotMapped]
        public string Url => $"https://{ActivityPubHostInformation.ApplicationHostname}/UserPosts/{Id}";

        private class ImageAdapter(Post post, Image image) : IActivityPubImage
        {
            public string Url => $"https://{ActivityPubHostInformation.ApplicationHostname}/Blobs/UserPosts/{post.Id}/{image.Raster.Id}";

            string IActivityPubImage.AltText => image.AltText ?? "";

            string IActivityPubImage.MediaType => image.Raster.ContentType;

            decimal? IActivityPubImage.HorizontalFocalPoint => image.FocalPoint?.Horizontal;

            decimal? IActivityPubImage.VerticalFocalPoint => image.FocalPoint?.Vertical;
        }

        public string GetImageUrl(Image image) =>
            new ImageAdapter(this, image).Url;

        Badge IPost.Badge => Badges.Pandacap;

        string IPost.DisplayTitle => !string.IsNullOrWhiteSpace(Title)
            ? Title
            : ExcerptGenerator.FromText(60, TextConverter.FromHtml(Html));

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => $"/UserPosts/{this.Id}";

        string IPost.ExternalUrl => $"/UserPosts/{this.Id}";

        DateTimeOffset IPost.PostedAt => PublishedTime;

        string? IPost.ProfileUrl => null;

        private class Thumbnail(Post post, Image image) : IPostThumbnail
        {
            string IPostThumbnail.Url => $"/Blobs/UserPosts/{post.Id}/{image.PrimaryThumbnail.Id}";
            string IPostThumbnail.AltText => image.AltText!;
        }

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Images.Select(i => new Thumbnail(this, i));

        string? IPost.Username => null;

        string? IPost.Usericon => null;

        IActivityPubAddressing IActivityPubPost.Addressing => this;

        bool IActivityPubPost.IsArticle => Type == PostType.JournalEntry;

        IEnumerable<string> IActivityPubPost.Tags => Tags;

        IEnumerable<IActivityPubLink> IActivityPubPost.Links => Links;

        IEnumerable<IActivityPubImage> IActivityPubPost.Images => Images.Select(image => new ImageAdapter(this, image));

        string IActivityPubPost.ObjectId => Url;

        string? IActivityPubAddressing.InReplyTo => null;

        IEnumerable<string> IActivityPubAddressing.To => ["https://www.w3.org/ns/activitystreams#Public"];

        IEnumerable<string> IActivityPubAddressing.Cc => [$"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Followers"];

        string? IActivityPubAddressing.Audience => null;

        string? IPlatformLinkPostSource.ActivityPubObjectId => Url;
    }
}
