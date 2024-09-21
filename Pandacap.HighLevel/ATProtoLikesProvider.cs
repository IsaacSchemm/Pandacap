using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Core;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.HighLevel
{
    public class ATProtoLikesProvider(
        ATProtoCredentialProvider atProtoCredentialProvider,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        private const string Key = "9be4f4c3-e2dc-45c3-98b8-b8ad0fb5df9f";

        private class CachedList
        {
            private readonly SemaphoreSlim _sem = new(1, 1);
            private readonly List<BlueskyFeed.FeedItem> _list = [];
            private BlueskyFeed.Page? _cursor = BlueskyFeed.Page.FromStart;

            public async IAsyncEnumerable<BlueskyFeed.FeedItem> EnumerateAsync(
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

                        _list.AddRange(response.feed);

                        _cursor = response.feed.Length > 0 && OptionModule.ToObj(response.cursor) is string next
                            ? BlueskyFeed.Page.NewFromCursor(next)
                            : null;

                        foreach (var item in response.feed)
                            yield return item;
                    }
                }
                finally
                {
                    _sem.Release();
                }
            }
        }

        public IAsyncEnumerable<BlueskyFeed.FeedItem> EnumerateAsync()
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
