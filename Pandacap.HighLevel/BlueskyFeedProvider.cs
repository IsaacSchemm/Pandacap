using Microsoft.FSharp.Core;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.HighLevel
{
    public static class BlueskyFeedProvider
    {
        public static async IAsyncEnumerable<BlueskyFeed.FeedItem> WrapAsync(
            Func<BlueskyFeed.Page, Task<BlueskyFeed.FeedResponse>> handler)
        {
            var page = BlueskyFeed.Page.FromStart;

            while (true)
            {
                var results = await handler(page);

                foreach (var item in results.feed)
                    yield return item;

                if (OptionModule.IsNone(results.cursor))
                    break;

                page = BlueskyFeed.Page.NewFromCursor(results.cursor.Value);
            }
        }
    }
}
