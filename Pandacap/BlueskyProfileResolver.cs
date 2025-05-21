using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Pandacap
{
    public class BlueskyProfileResolver(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ApplicationInformation appInfo,
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
            public required string DID { get; init; }
            public required string Handle { get; init; }
            public required Uri BlueskyUri { get; init; }
            public required FSharpList<ActivityPubMirror> ActivityPubMirrors { get; init; }
        }

        private async Task<FSharpList<ActivityPubMirror>> FindBridgesToActivityPub(string did)
        {
            var addressee = await activityPubRemoteActorService.FetchAddresseeAsync(
                $"https://bsky.brid.gy/ap/{Uri.EscapeDataString(did)}",
                CancellationToken.None);
            return addressee is RemoteAddressee.Actor actor
                ? [
                    new()
                    {
                        Id = actor.Id,
                        Handle = $"@{actor.Item.PreferredUsername}@bsky.brid.gy"
                    }
                ]
                : [];
        }

        private async Task<FSharpList<ProfileInformation>> ResolveProfileAsync(string didOrHandle)
        {
            try
            {
                using var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                var profile = await Profile.GetProfileAsync(
                    client,
                    didOrHandle);

                var addressee = await activityPubRemoteActorService.FetchAddresseeAsync(
                    $"https://bsky.brid.gy/ap/{Uri.EscapeDataString(profile.handle)}",
                    CancellationToken.None);

                return [
                    new()
                    {
                        DID = profile.did,
                        Handle = profile.handle,
                        BlueskyUri = new Uri($"https://bsky.app/profile/{Uri.EscapeDataString(profile.handle)}"),
                        ActivityPubMirrors = profile.handle.EndsWith("@bsky.brid.gy")
                            ? []
                            : await FindBridgesToActivityPub(profile.did)
                    }
                ];
            }
            catch (Exception)
            {
                return [];
            }
        }

        private async Task<FSharpList<ProfileInformation>> GetAsync(string did)
        {
            string key = $"{nameof(GetAsync)} {did}";
            if (memoryCache.TryGetValue(key, out var found) && found is FSharpList<ProfileInformation> pi)
                return pi;

            return memoryCache.Set(
                key,
                await ResolveProfileAsync(did),
                DateTimeOffset.UtcNow.AddHours(1));
        }

        public async Task<FSharpList<ProfileInformation>> GetAsync(IEnumerable<string> dids)
        {
            var results = await Task.WhenAll(
                dids
                .Select(GetAsync));

            return [.. results.SelectMany(pi => pi)];
        }
    }
}
