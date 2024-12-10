using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class FurAffinityTimeZoneCache(PandacapDbContext context)
    {
        private readonly Lazy<Task<TimeZoneInfo>> _timeZoneInfo = new(async () =>
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                return TimeZoneInfo.Utc;

            return await FurAffinity.GetTimeZoneAsync(credentials, CancellationToken.None);
        });

        public async Task<FurAffinityTimeZoneConverter> GetConverterAsync() =>
            new(await _timeZoneInfo.Value);
    }
}
