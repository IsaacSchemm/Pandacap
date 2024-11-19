using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching JSON from FAExport")]
    public class FAExportClient(
        ApplicationInformation appInfo,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        private async Task<HttpClient> CreateClientAsync(CancellationToken cancellationToken)
        {
            var credentials = await context.FurAffinityCredentials.SingleAsync(cancellationToken);

            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new("https://faexport.spangle.org.uk");
            client.DefaultRequestHeaders.Add("FA_COOKIE", $"b={credentials.B}; a={credentials.A}");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);
            return client;
        }

        public record FolderSubmission(
            int id,
            string title,
            string thumbnail,
            string link,
            string name,
            string profile,
            string profile_name);

        public async Task<FSharpList<FolderSubmission>> PageUserFolderAsync(
            string username,
            bool scraps = false,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            string folder = scraps ? "scraps" : "gallery";

            using var client = await CreateClientAsync(cancellationToken);
            using var resp = await client.GetAsync(
                $"/user/{Uri.EscapeDataString(username)}/{Uri.EscapeDataString(folder)}.json?page={page}&full=1",
                cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<FSharpList<FolderSubmission>>(cancellationToken) ?? [];
        }

        public async IAsyncEnumerable<FolderSubmission> GetUserFolderAsync(
            string username,
            bool scraps = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (int page = 1; ; page++)
            {
                var results = await PageUserFolderAsync(username, scraps, page, cancellationToken);
                if (results.Length == 0)
                    break;

                foreach (var item in results)
                    yield return item;
            }
        }

        //public record Submission(
        //    string title,
        //    string description,
        //    string description_body,
        //    string name,
        //    string profile,
        //    string profile_name,
        //    string avatar,
        //    string link,
        //    string posted,
        //    DateTimeOffset posted_at,
        //    string download,
        //    string full,
        //    string thumbnail,
        //    string gallery,
        //    string category,
        //    string theme,
        //    string species,
        //    string gender,
        //    int favorites,
        //    int comments,
        //    int views,
        //    string resolution,
        //    string rating,
        //    FSharpList<string> keywords);

        //public async Task<Submission?> GetSubmissionAsync(
        //    int id,
        //    CancellationToken cancellationToken = default)
        //{
        //    using var client = await CreateClientAsync(cancellationToken);
        //    using var resp = await client.GetAsync(
        //        $"/submission/{id}.json",
        //        cancellationToken);
        //    if (resp.StatusCode == HttpStatusCode.NotFound)
        //        return null;
        //    resp.EnsureSuccessStatusCode();
        //    return await resp.Content.ReadFromJsonAsync<Submission>(cancellationToken);
        //}

        public record Journal(
            string url);

        public async Task<Journal?> PostJournalAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
        {
            using var client = await CreateClientAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, "/journal.json");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["title"] = title,
                ["description"] = description
            });
            using var resp = await client.SendAsync(req, cancellationToken);
            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<Journal>(cancellationToken);
        }
    }
}
