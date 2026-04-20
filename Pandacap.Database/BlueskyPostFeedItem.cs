namespace Pandacap.Database
{
    public class BlueskyPostFeedItem : BlueskyFeedItem
    {
        public string RecordKey { get; set; } = "";

        public User Author { get; set; } = new();

        public override bool IsShare => false;

        public override string OriginalDID => Author.DID;

        public override string OriginalPDS => Author.PDS;

        public override string OriginalRecordKey => RecordKey;

        public override User AttributeTo => Author;

        public override DateTimeOffset DateTo => CreatedAt;
    }
}
