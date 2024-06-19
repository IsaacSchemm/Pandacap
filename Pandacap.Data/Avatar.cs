namespace Pandacap.Data
{
    public class Avatar
    {
        public Guid Id { get; set; }

        public string ContentType { get; set; } = "application/octet-stream";

        public string BlobName => $"{Id}";
    }
}
