namespace Pandacap.Functions
{
    public static class DateTimeOffsetUtil
    {
        public static DateTimeOffset Max(DateTimeOffset dt1, DateTimeOffset? dt2)
        {
            return dt2 is DateTimeOffset x && x > dt1
                ? x
                : dt1;
        }
    }
}
