using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pandacap.Database
{
    public class Upload : IPost, IPostThumbnail
    {
        public Guid Id { get; set; }

        public string ContentType { get; set; } = "application/octet-stream";

        public Guid? Raster { get; set; }

        public string AltText { get; set; } = "";

        public DateTimeOffset UploadedAt { get; set; }

        Badge IPost.Badge => Badges.Pandacap;

        string IPost.DisplayTitle => $"{UploadedAt.UtcDateTime:D} {UploadedAt.UtcDateTime:t}";

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => $"/Uploads/{Id}";

        string IPost.ExternalUrl => $"/Uploads/{Id}";

        DateTimeOffset IPost.PostedAt => UploadedAt;

        string IPost.ProfileUrl => null!;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => [this];

        string IPost.Username => null!;

        string IPost.Usericon => null!;

        string IPostThumbnail.Url => $"/Blobs/Uploads/{Id}";
    }
}
