using Pandacap.FurAffinity;
using System.Text.RegularExpressions;

namespace Pandacap.HighLevel.FurAffinity
{
    public static partial class FurAffinityExtensions
    {
        public static DateTimeOffset? GetPublishedTime(this FA.Submission submission) =>
            GetFurAffinityThumbnailPattern().Match(submission.thumbnail) is Match match
            && match.Success
            && long.TryParse(match.Groups[1].Value, out long ts)
                ? DateTimeOffset.FromUnixTimeSeconds(ts)
                : null;

        [GeneratedRegex(@"^https://t.furaffinity.net/[0-9]+@[0-9]+-([0-9]+)")]
        private static partial Regex GetFurAffinityThumbnailPattern();
    }
}
