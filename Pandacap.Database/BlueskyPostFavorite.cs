using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class BlueskyPostFavorite : IFavorite
    {
        public Guid Id { get; set; } = Guid.Empty;

        public string CID { get; set; } = "";

        public string RecordKey { get; set; } = "";

        public class User
        {
            public string? PDS { get; set; }
            public string DID { get; set; } = "";
            public string Handle { get; set; } = "";
        }

        public User CreatedBy { get; set; } = new();

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset FavoritedAt { get; set; }

        public DateTimeOffset? HiddenAt { get; set; }

        public string Text { get; set; } = "";

        public class Image
        {
            public string CID { get; set; } = "";
            public string Alt { get; set; } = "";
        }

        public List<Image> Images { get; set; } = [];

        Badge IPost.Badge => Badges.ATProto.WithText(CreatedBy.PDS ?? "public.api.bsky.app");

        string IPost.DisplayTitle => Text;

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => $"/ATProto/ViewBlueskyPost?did={CreatedBy.DID}&rkey={RecordKey}";

        string IPost.ExternalUrl => $"https://bsky.app/profile/{CreatedBy.DID}/post/{RecordKey}";

        DateTimeOffset IPost.PostedAt => CreatedAt;

        string? IPost.ProfileUrl => $"https://bsky.app/profile/{CreatedBy.DID}";

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Images.Select(image => new PostThumbnailAdapter(CreatedBy, image));

        string? IPost.Username => CreatedBy.Handle;

        string? IPost.Usericon => null;

        private class PostThumbnailAdapter(User user, Image image) : IPostThumbnail
        {
            string IPostThumbnail.Url => $"/ATProto/GetBlob?did={user.DID}&cid={image.CID}";

            string IPostThumbnail.AltText => image.Alt;
        }
    }
}
