using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Pandacap
{
    public class BlueskyProfileResolver(
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
            try
            {
                using var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                using var req = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://bsky.brid.gy/ap/{Uri.EscapeDataString(did)}");

                req.Headers.Accept.ParseAdd("application/activity+json");

                using var resp = await client.SendAsync(req);

                if (!resp.IsSuccessStatusCode)
                    return [];

                async Task<T?> deserializeAsync<T>(T _)
                {
                    return await resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<T>();
                }

                var actor = await deserializeAsync(new
                {
                    id = "",
                    preferredUsername = ""
                });

                if (actor == null)
                    return [];

                return [
                    new()
                    {
                        Id = actor.id,
                        Handle = $"@{actor.preferredUsername}@bsky.brid.gy"
                    }
                ];
            }
            catch (Exception)
            {
                return [];
            }
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
