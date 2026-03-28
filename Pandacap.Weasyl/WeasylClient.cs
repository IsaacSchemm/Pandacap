using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.FSharp.Collections;
using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Models.WeasylApi;
using Pandacap.Weasyl.Models.WeasylUpload;
using Pandacap.Weasyl.Scraping.Interfaces;
using Pandacap.Weasyl.Scraping.Models;

namespace Pandacap.Weasyl
{
    internal partial class WeasylClient(
        HttpMessageHandler httpMessageHandler,
        string apiKey,
        string phpProxyHost,
        IWeasylScraper weasylScraper): IWeasylClient
    {
        private HttpClient CreateClient()
        {
            var client = new HttpClient(httpMessageHandler, disposeHandler: false);

            client.DefaultRequestHeaders.Add(
                "X-Weasyl-API-Key",
                apiKey);

            return client;
        }

        private Uri WeasylProxyHost => new("https://" + phpProxyHost);
        private Uri WeasylProxy => new(WeasylProxyHost, "/pandacap/weasyl_proxy.php");
        private Uri WeasylSubmit => new(WeasylProxyHost, "/pandacap/weasyl_submit.php");

        [GeneratedRegex(@"<option value=""(\d+)"">([^<]+)</option>")]
        private static partial Regex OptionTag();

        [GeneratedRegex(@"/[^/]*/submissions?/([0-9]+)/")]
        private static partial Regex SubmissionUri();

        [GeneratedRegex(@"/journal/([0-9]+)/")]
        private static partial Regex JournalUri();

        public async Task<WhoamiResponse> WhoamiAsync(CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=api/whoami", cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WhoamiResponse>(cancellationToken)
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public async Task<AvatarResponse> GetAvatarAsync(string username, CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=api/useravatar&username={Uri.EscapeDataString(username)}", cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<AvatarResponse>(cancellationToken)
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public async Task<Submission> ViewSubmissionAsync(int submitid, CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=api/submissions/{submitid}/view&anyway=x", cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<Submission>(cancellationToken)
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        private async Task<SubmissionsResponse> PageMessagesSubmissionsAsync(long? nexttime = null, CancellationToken cancellationToken = default)
        {
            string qs = nexttime == null
                ? ""
                : $"nexttime={nexttime}";

            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path={Uri.EscapeDataString($"api/messages/submissions?{qs}")}", cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<SubmissionsResponse>(cancellationToken)
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public async IAsyncEnumerable<Submission> GetMessagesSubmissionsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var resp = await PageMessagesSubmissionsAsync(cancellationToken: cancellationToken);

            while (true)
            {
                foreach (var submission in resp.submissions)
                    yield return submission;

                if (resp.nexttime is long nexttime)
                    resp = await PageMessagesSubmissionsAsync(
                        nexttime: nexttime,
                        cancellationToken: cancellationToken);
                else
                    break;
            }
        }

        public async Task<MessagesSummary> GetMessagesSummaryAsync(CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=api/messages/summary", cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<MessagesSummary>(cancellationToken)
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public async IAsyncEnumerable<int> ExtractFavoriteSubmitidsAsync(int userid, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            int? nextid = null;

            while (true)
            {
                string qs = $"userid={userid}&feature=submit";
                if (nextid is int n)
                    qs += $"&nextid={n}";

                using var client = CreateClient();
                using var resp = await client.GetAsync($"{WeasylProxy}?path=favorites&{qs}", cancellationToken);
                resp.EnsureSuccessStatusCode();
                string html = await resp.Content.ReadAsStringAsync(cancellationToken);
                var page = weasylScraper.ExtractFavoriteSubmitids(html);

                foreach (int submitid in page.submitids)
                    yield return submitid;

                if (page.nextid == null)
                    break;

                nextid = page.nextid;
            }
        }

        public async Task<FSharpList<ExtractedJournal>> ExtractJournalsAsync(CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=messages/notifications", cancellationToken);
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(cancellationToken);
            return weasylScraper.ExtractJournals(html);
        }

        public async Task<FSharpList<ExtractedNotification>> ExtractNotificationsAsync(CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=messages/notifications", cancellationToken);
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(cancellationToken);
            return weasylScraper.ExtractNotifications(html);
        }

        public async Task<FSharpList<ExtractedNote>> GetNotesAsync(CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=notes", cancellationToken);
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(cancellationToken);
            return weasylScraper.ExtractNotes(html);
        }

        public async IAsyncEnumerable<Folder> GetFoldersAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=submit/visual", cancellationToken);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var sr = new StreamReader(stream);

            string? line;
            while ((line = await sr.ReadLineAsync(cancellationToken)) != null)
            {
                if (line.Contains("<select name=\"folderid\""))
                {
                    break;
                }
            }

            while ((line = await sr.ReadLineAsync(cancellationToken)) != null)
            {
                var match = OptionTag().Match(line);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
                {
                    yield return new(
                        id,
                        match.Groups[2].Value);
                }
                if (line.Contains("</select>"))
                    break;
            }
        }

        public async Task<int?> UploadVisualAsync(
            string url,
            string title,
            SubmissionType subtype,
            int? folderid,
            Rating rating,
            string content,
            IEnumerable<string> tags,
            CancellationToken cancellationToken)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, WeasylSubmit);

            req.Content = new MultipartFormDataContent {
                { new StringContent(url), "submitfile" },
                { new StringContent(title), "title" },
                { new StringContent($"{(int)subtype}"), "subtype" },
                { new StringContent($"{folderid}"), "folderid" },
                { new StringContent($"{(int)rating}"), "rating" },
                { new StringContent($"{content}"), "content" },
                { new StringContent(string.Join(" ", tags.Select(s => s.Replace(' ', '_')))), "tags" },
            };

            using var client = CreateClient();
            using var resp = await client.SendAsync(req, cancellationToken);
            resp.EnsureSuccessStatusCode();

            async IAsyncEnumerable<string> readResponseContentAsync()
            {
                if (resp.Content.Headers.ContentType?.MediaType != "text/uri-list")
                    yield break;

                using var sr = new StreamReader(await resp.Content.ReadAsStreamAsync(cancellationToken));
                string? line;
                while ((line = await sr.ReadLineAsync(cancellationToken)) != null)
                    yield return line;
            }

            if (await readResponseContentAsync().SingleOrDefaultAsync(cancellationToken) is not string uri)
                throw new Exception("Expected a single URI from the PHP proxy");

            var match = SubmissionUri().Match(uri);
            return match.Success && int.TryParse(match.Groups[1].Value, out int submitid)
                ? submitid
                : null;
        }

        public async Task<int?> UploadJournalAsync(
            string title,
            Rating rating,
            string content,
            IEnumerable<string> tags,
            CancellationToken cancellationToken)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{WeasylProxy}?path=submit/journal");

            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["title"] = title,
                ["rating"] = $"{(int)rating}",
                ["content"] = content,
                ["tags"] = string.Join(" ", tags.Select(s => s.Replace(' ', '_')))
            });

            using var client = CreateClient();
            using var resp = await client.SendAsync(req, cancellationToken);

            var uri = resp.Headers.Location
                ?? resp.EnsureSuccessStatusCode().RequestMessage?.RequestUri;

            var match = JournalUri().Match(uri?.LocalPath ?? "");
            return match.Success && int.TryParse(match.Groups[1].Value, out int journalid)
                ? journalid
                : null;
        }
    }
}
