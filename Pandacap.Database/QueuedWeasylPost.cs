using Pandacap.Weasyl.Models.WeasylUpload;

namespace Pandacap.Database
{
    public class QueuedWeasylPost
    {
        public Guid PostId { get; set; }

        public SubmissionType Subtype { get; set; }

        public int? FolderId { get; set; }

        public Rating Rating { get; set; }
    }
}
