namespace Pandacap.Database
{
    /// <summary>
    /// A remote ActivityPub post that is a reply to one of this app's posts (a UserPost or AddressedPost).
    /// </summary>
    public class RemoteActivityPubReply
    {
        public Guid Id { get; set; }

        public string ObjectId { get; set; } = "";

        public string InReplyTo { get; set; } = "";

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
