using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    public abstract class BlueskyShareFeedItem : BlueskyFeedItem
    {
        public class OriginalPost
        {
            public string CID { get; set; } = "";
            public string DID { get; set; } = "";
            public string PDS { get; set; } = "";
            public string RecordKey { get; set; } = "";
        }

        public OriginalPost Original { get; set; } = new();

        [NotMapped]
        public abstract User SharedBy { get; }

        [NotMapped]
        public abstract DateTimeOffset SharedAt { get; }

        public override string OriginalDID => Original.DID;

        public override string OriginalPDS => Original.PDS;

        public override string OriginalRecordKey => Original.RecordKey;

        public override User AttributeTo => SharedBy;

        public override DateTimeOffset DateTo => SharedAt;
    }
}
