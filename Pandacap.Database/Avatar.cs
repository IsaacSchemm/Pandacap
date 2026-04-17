using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    public class Avatar
    {
        public Guid Id { get; set; }

        public string ContentType { get; set; } = "application/octet-stream";

        [NotMapped]
        public string BlobName => $"{Id}"
;    }
}
