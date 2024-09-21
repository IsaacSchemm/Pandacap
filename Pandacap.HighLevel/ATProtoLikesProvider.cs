using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Core;
using Pandacap.Data;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.HighLevel
{
    public class ATProtoLikesProvider(
        ATProtoCredentialProvider atProtoCredentialProvider,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        private const string Key = "9be4f4c3-e2dc-45c3-98b8-b8ad0fb5df9f";

        private record BlueskyPostWrapper(BlueskyFeed.FeedItem Item) : IPost
        {
            private IEnumerable<string> EnumerateText()
            {
                yield return Item.post.record.text;
                foreach (var image in Item.post.Images)
                    yield return image.alt;
            }

            string IPost.Id => Item.post.cid;
            string IPost.Username => Item.post.author.DisplayNameOrNull ?? Item.post.author.did;
            string IPost.Usericon => Item.post.author.AvatarOrNull;
            string IPost.DisplayTitle => ExcerptGenerator.FromText(EnumerateText());
            DateTimeOffset IPost.Timestamp => Item.post.indexedAt;
            string IPost.LinkUrl => $"https://bsky.app/profile/{Item.post.author.did}/post/{Item.post.RecordKey}";
            IEnumerable<string> IPost.ThumbnailUrls => Item.post.Images.Select(i => i.thumb);
        }

        private class CachedList
        {
            private readonly SemaphoreSlim _sem = new(1, 1);
            private readonly List<BlueskyPostWrapper> _list = [];
            private BlueskyFeed.Page? _cursor = BlueskyFeed.Page.FromStart;

            public async IAsyncEnumerable<BlueskyPostWrapper> EnumerateAsync(
                ATProtoCredentialProvider atProtoCredentialProvider,
                IHttpClientFactory httpClientFactory)
            {
                await _sem.WaitAsync();

                try
                {
                    foreach (var item in _list)
                        yield return item;

                    using var client = httpClientFactory.CreateClient();

                    if (await atProtoCredentialProvider.GetCredentialsAsync() is not ATProtoCredentialProvider.AutomaticRefreshCredentials credentials)
                        yield break;

                    while (_cursor != null)
                    {
                        var response = await BlueskyFeed.GetActorLikesAsync(
                            client,
                            credentials,
                            credentials.DID,
                            _cursor);

                        var page = response.feed
                            .Select(item => new BlueskyPostWrapper(item))
                            .ToList();

                        _list.AddRange(page);

                        _cursor = response.feed.Length > 0 && OptionModule.ToObj(response.cursor) is string next
                            ? BlueskyFeed.Page.NewFromCursor(next)
                            : null;

                        foreach (var item in page)
                            yield return item;
                    }
                }
                finally
                {
                    _sem.Release();
                }
            }
        }

        public IAsyncEnumerable<IPost> EnumerateAsync()
        {
            var cachedList = memoryCache.GetOrCreate(Key, cacheEntry =>
            {
                cacheEntry.SetSlidingExpiration(TimeSpan.FromHours(1));
                return new CachedList();
            });

            return cachedList!.EnumerateAsync(
                atProtoCredentialProvider,
                httpClientFactory);
        }
    }
}
