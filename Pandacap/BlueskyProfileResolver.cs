using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using System.Runtime.CompilerServices;

namespace Pandacap
{
    public class BlueskyProfileResolver(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        public record ActivityPubMirror
        {
            public required string Id { get; init; }
            public required string Handle { get; init; }
        }

        public record ProfileInformation
        {
            public required string Handle { get; init; }
            public required Uri BlueskyUri { get; init; }
            public required FSharpList<ActivityPubMirror> ActivityPubMirrors { get; init; }
        }

        public async Task<ProfileInformation> ResolveAsync(string did)
        {
            string key = $"{nameof(ResolveAsync)} {did}";
            if (memoryCache.TryGetValue(key, out var found) && found is ProfileInformation pi)
                return pi;

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var profile = await Profile.GetProfileAsync(
                client,
                did);

            async IAsyncEnumerable<ActivityPubMirror> findMirrorsAsync()
            {
                var addressee = await activityPubRemoteActorService.FetchAddresseeAsync(
                    $"https://bsky.brid.gy/ap/{Uri.EscapeDataString(profile.handle)}",
                    CancellationToken.None);
                if (addressee is RemoteAddressee.Actor actor)
                    yield return new()
                    {
                        Id = actor.Id,
                        Handle = $"@{actor.Item.PreferredUsername}@bsky.brid.gy"
                    };
            }

            var addressee = await activityPubRemoteActorService.FetchAddresseeAsync(
                $"https://bsky.brid.gy/ap/{Uri.EscapeDataString(profile.handle)}",
                CancellationToken.None);

            var profileInformation = new ProfileInformation
            {
                Handle = profile.handle,
                BlueskyUri = new Uri($"https://bsky.app/profile/{Uri.EscapeDataString(profile.handle)}"),
                ActivityPubMirrors = [.. await findMirrorsAsync().ToListAsync()]
            };

            memoryCache.Set(key, profileInformation, DateTimeOffset.UtcNow.AddHours(1));

            return profileInformation;
        }

        public async IAsyncEnumerable<ProfileInformation> ResolveAllAsync(
            params IAsyncEnumerable<string>[] asyncSeqs)
        {
            foreach (var asyncSeq in asyncSeqs)
                await foreach (var did in asyncSeq)
                    yield return await ResolveAsync(did);
        }
    }
}
