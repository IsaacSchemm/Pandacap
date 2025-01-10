using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;

namespace Pandacap.HighLevel.FurAffinity
{
    public class FurAffinityTimeZoneCache(PandacapDbContext context)
    {
        private readonly Lazy<Task<TimeZoneInfo>> _timeZoneInfo = new(async () =>
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                return TimeZoneInfo.Utc;

            return await FA.GetTimeZoneAsync(credentials, CancellationToken.None);
        });

        public async Task<FurAffinityTimeZoneConverter> GetConverterAsync() =>
            new(await _timeZoneInfo.Value);
    }
}
