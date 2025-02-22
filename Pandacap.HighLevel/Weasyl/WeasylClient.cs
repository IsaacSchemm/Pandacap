using Microsoft.FSharp.Collections;
using Pandacap.ConfigurationObjects;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Pandacap.Html;

namespace Pandacap.HighLevel
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching response JSON from Weasyl")]
    public partial class WeasylClient(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory,
        string apiKey)
    {
        private HttpClient CreateClient()
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add(
                "X-Weasyl-API-Key",
                apiKey);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);
            return client;
        }

        private Uri WeasylProxyHost => new("https://" + appInfo.WeasylProxyHost);
        private Uri WeasylProxy => new(WeasylProxyHost, "/pandacap/weasyl_proxy.php");
        private Uri WeasylSubmit => new(WeasylProxyHost, "/pandacap/weasyl_submit.php");

        [GeneratedRegex(@"<option value=""(\d+)"">([^<]+)</option>")]
        private static partial Regex OptionTag();

        [GeneratedRegex(@"/[^/]*/submissions?/([0-9]+)/")]
        private static partial Regex SubmissionUri();

        [GeneratedRegex(@"/journal/([0-9]+)/")]
        private static partial Regex JournalUri();

        public record WhoamiResponse(
            string login,
            int userid);

        public async Task<WhoamiResponse> WhoamiAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=api/whoami");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WhoamiResponse>()
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public record AvatarResponse(
            string avatar);

        public async Task<AvatarResponse> GetAvatarAsync(string username)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=api/useravatar&username={Uri.EscapeDataString(username)}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<AvatarResponse>()
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        private record SubmissionsResponse(
            long? backtime,
            long? nexttime,
            FSharpList<Submission> submissions);

        public record Submission(
            int submitid,
            string title,
            string rating,
            DateTimeOffset posted_at,
            string type,
            string owner,
            string owner_login,
            OwnerMedia? owner_media,
            SubmissionMedia media,
            string link);

        public record SubmissionMedia(
            FSharpList<Media> thumbnail);

        public record OwnerMedia(
            FSharpList<Media> avatar);

        public record Media(
            string url);

        public async Task<Submission> ViewSubmissionAsync(int submitid)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=api/submissions/{submitid}/view&anyway=x");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<Submission>()
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        private async Task<SubmissionsResponse> PageMessagesSubmissionsAsync(long? nexttime = null)
        {
            string qs = nexttime == null
                ? ""
                : $"nexttime={nexttime}";

            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path={Uri.EscapeDataString($"api/messages/submissions?{qs}")}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<SubmissionsResponse>()
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public async IAsyncEnumerable<Submission> GetMessagesSubmissionsAsync()
        {
            var resp = await PageMessagesSubmissionsAsync();

            while (true)
            {
                foreach (var submission in resp.submissions)
                    yield return submission;

                if (resp.nexttime is long nexttime)
                    resp = await PageMessagesSubmissionsAsync(nexttime: nexttime);
                else
                    break;
            }
        }

        public record MessagesSummary(
            int comments,
            int journals,
            int notifications,
            int submissions,
            int unread_notes);

        public async Task<MessagesSummary> GetMessagesSummaryAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=api/messages/summary");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<MessagesSummary>()
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public async IAsyncEnumerable<int> ExtractFavoriteSubmitidsAsync(int userid)
        {
            int? nextid = null;

            while (true)
            {
                string qs = $"userid={userid}&feature=submit";
                if (nextid is int n)
                    qs += $"&nextid={n}";

                using var client = CreateClient();
                using var resp = await client.GetAsync($"{WeasylProxy}?path=favorites&{qs}");
                resp.EnsureSuccessStatusCode();
                string html = await resp.Content.ReadAsStringAsync();
                var page = WeasylScraper.ExtractFavoriteSubmitids(html);

                foreach (int submitid in page.submitids)
                    yield return submitid;

                if (page.nextid == null)
                    break;

                nextid = page.nextid;
            }
        }

        public async Task<FSharpList<WeasylScraper.ExtractedJournal>> ExtractJournalsAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=messages/notifications");
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync();
            return WeasylScraper.ExtractJournals(html);
        }

        public async Task<FSharpList<WeasylScraper.ExtractedNotification>> ExtractNotificationsAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=messages/notifications");
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync();
            return WeasylScraper.ExtractNotifications(html);
        }

        public async Task<FSharpList<WeasylScraper.Note>> GetNotesAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=notes");
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync();
            return WeasylScraper.ExtractNotes(html);
        }

        public record Folder(int FolderId, string Name)
        {
            public override string ToString()
            {
                return $"{Name} ({FolderId})";
            }
        }

        public async IAsyncEnumerable<Folder> GetFoldersAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{WeasylProxy}?path=submit/visual");
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var sr = new StreamReader(stream);

            string? line;
            while ((line = await sr.ReadLineAsync()) != null)
            {
                if (line.Contains("<select name=\"folderid\""))
                {
                    break;
                }
            }

            while ((line = await sr.ReadLineAsync()) != null)
            {
                var match = OptionTag().Match(line);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
                {
                    yield return new Folder(
                        id,
                        match.Groups[2].Value);
                }
                if (line.Contains("</select>"))
                    break;
            }
        }

        public enum SubmissionType
        {
            Sketch = 1010,
            Traditional = 1020,
            Digital = 1030,
            Animation = 1040,
            Photography = 1050,
            Design_Interface = 1060,
            Modeling_Sculpture = 1070,
            Crafts_Jewelry = 1075,
            Sewing_Knitting = 1078,
            Desktop_Wallpaper = 1080,
            Other = 1999,
        }

        public enum Rating
        {
            General = 10,
            Mature = 30,
            Explicit = 40,
        }

        public async Task<int?> UploadVisualAsync(string url, string title, SubmissionType subtype, int? folderid, Rating rating, string content, IEnumerable<string> tags)
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
            using var resp = await client.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            async IAsyncEnumerable<string> readResponseContentAsync()
            {
                if (resp.Content.Headers.ContentType?.MediaType != "text/uri-list")
                    yield break;

                using var sr = new StreamReader(await resp.Content.ReadAsStreamAsync());
                string? line;
                while ((line = await sr.ReadLineAsync()) != null)
                    yield return line;
            }

            if (await readResponseContentAsync().SingleOrDefaultAsync() is not string uri)
                throw new Exception("Expected a single URI from the PHP proxy");

            var match = SubmissionUri().Match(uri);
            return match.Success && int.TryParse(match.Groups[1].Value, out int submitid)
                ? submitid
                : null;
        }

        public async Task<int?> UploadJournalAsync(string title, Rating rating, string content, IEnumerable<string> tags)
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
            using var resp = await client.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var match = JournalUri().Match(resp.RequestMessage?.RequestUri?.LocalPath ?? "");
            return match.Success && int.TryParse(match.Groups[1].Value, out int journalid)
                ? journalid
                : null;
        }
    }
}
