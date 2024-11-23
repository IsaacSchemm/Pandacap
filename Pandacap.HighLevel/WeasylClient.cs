using Microsoft.FSharp.Collections;
using Pandacap.LowLevel;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

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

        [GeneratedRegex(@"<option value=""(\d+)"">([^<]+)</option>")]
        private static partial Regex OptionTag();

        [GeneratedRegex(@"^/~[^/]*/submissions/([0-9]+)/")]
        private static partial Regex SubmissionUri();

        [GeneratedRegex(@"^/journal/([0-9]+)/")]
        private static partial Regex JournalUri();

        public record WhoamiResponse(
            string login,
            int userid);

        public async Task<WhoamiResponse> WhoamiAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{appInfo.WeasylProxy}?path=api/whoami");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WhoamiResponse>()
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public record AvatarResponse(
            string avatar);

        public async Task<AvatarResponse> GetAvatarAsync(string username)
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{appInfo.WeasylProxy}?path=api/useravatar&username={Uri.EscapeDataString(username)}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<AvatarResponse>()
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        private record SubmissionsResponse(
            string? backtime,
            string? nexttime,
            FSharpList<Submission> submissions);

        public record Submission(
            int submitid,
            string title,
            string rating,
            DateTimeOffset posted_at,
            string type,
            string owner,
            string owner_login,
            SubmissionMedia media,
            string link);

        public record SubmissionMedia(
            FSharpList<Media> thumbnail);

        public record Media(
            string url,
            int mediaid);

        private async Task<SubmissionsResponse> PageMessagesSubmissionsAsync(string? nexttime = null)
        {
            string qs = nexttime == null
                ? ""
                : $"nexttime={Uri.EscapeDataString(nexttime)}";

            using var client = CreateClient();
            using var resp = await client.GetAsync($"{appInfo.WeasylProxy}?path={Uri.EscapeDataString($"api/messages/submissions?{qs}")}");
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

                if (resp.nexttime is string nexttime)
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
            using var resp = await client.GetAsync($"{appInfo.WeasylProxy}?path=api/messages/summary");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<MessagesSummary>()
                ?? throw new Exception($"Null response from {resp.RequestMessage?.RequestUri}");
        }

        public async Task<FSharpList<WeasylScraper.NotificationGroup>> GetNotificationsAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{appInfo.WeasylProxy}?path=messages/notifications");
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync();
            return WeasylScraper.ExtractNotifications(html);
        }

        public async Task<FSharpList<WeasylScraper.Note>> GetNotesAsync()
        {
            using var client = CreateClient();
            using var resp = await client.GetAsync($"{appInfo.WeasylProxy}?path=notes");
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
            using var resp = await client.GetAsync($"{appInfo.WeasylProxy}?path=submit/visual");
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

        public async Task<int?> UploadVisualAsync(ReadOnlyMemory<byte> data, string title, SubmissionType subtype, int? folderid, Rating rating, string content, IEnumerable<string> tags)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"https://www.weasyl.com/submit/visual");

            req.Content = new MultipartFormDataContent {
                { new ReadOnlyMemoryContent(data), "submitfile", "picture.dat" },
                { new ByteArrayContent([]), "thumbfile", "thumb.dat" },
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

            var match = SubmissionUri().Match(resp.RequestMessage?.RequestUri?.LocalPath ?? "");
            return match.Success && int.TryParse(match.Groups[1].Value, out int submitid)
                ? submitid
                : null;
        }

        public async Task<int?> UploadJournalAsync(string title, Rating rating, string content, IEnumerable<string> tags)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{appInfo.WeasylProxy}?path=submit/journal");

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
