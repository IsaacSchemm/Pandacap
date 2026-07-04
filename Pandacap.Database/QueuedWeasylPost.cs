using Pandacap.Weasyl.Models.WeasylUpload;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class QueuedWeasylPost
    {
        [Key]
        public Guid PostId { get; set; }

        public SubmissionType Subtype { get; set; }

        public int? FolderId { get; set; }

        public Rating Rating { get; set; }
    }
}
