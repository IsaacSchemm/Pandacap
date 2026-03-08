using Pandacap.Data;
using Pandacap.PlatformBadges;

namespace Pandacap.HighLevel.VectorSearch
{
    public record VectorSearchDisplayResult(IPost Post, double? Score) : IPost
    {
        public PostPlatform Platform => Post.Platform;
        public string Url => Post.Url;
        public string DisplayTitle => Post.DisplayTitle;
        public string Id => Post.Id;
        public string InternalUrl => Post.InternalUrl;
        public string ExternalUrl => Post.ExternalUrl;
        public DateTimeOffset PostedAt => Post.PostedAt;
        public string ProfileUrl => Post.ProfileUrl;
        public IEnumerable<IPostThumbnail> Thumbnails => Post.Thumbnails;
        public string Username => Post.Username;
        public string Usericon => Post.Usericon;
    }
}
