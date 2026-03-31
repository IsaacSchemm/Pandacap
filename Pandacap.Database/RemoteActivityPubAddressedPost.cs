namespace Pandacap.Database
{
    /// <summary>
    /// A remote ActivityPub post that is addressed to the Pandacap user but is not a reply to one of their posts.
    /// </summary>
    public class RemoteActivityPubAddressedPost
    {
        public Guid Id { get; set; }

        public string ObjectId { get; set; } = "";

        public string CreatedBy { get; set; } = "";

        public string? Username { get; set; }

        public string? Usericon { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string? Summary { get; set; }

        public bool Sensitive { get; set; }

        public string? Name { get; set; }

        public string? HtmlContent { get; set; }
    }
}
