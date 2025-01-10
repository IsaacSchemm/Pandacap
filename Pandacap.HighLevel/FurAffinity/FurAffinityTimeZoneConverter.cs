namespace Pandacap.HighLevel.FurAffinity
{
    public class FurAffinityTimeZoneConverter(TimeZoneInfo timeZoneInfo)
    {
        public DateTimeOffset ConvertToUtc(DateTime dateTime) =>
            TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified),
                timeZoneInfo);
    }
}
