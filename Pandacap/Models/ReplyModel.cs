namespace Pandacap.Models
{
    public class ReplyModel
    {
        public required bool Remote { get; init; }

        public required string ObjectId { get; init; }
        public required string CreatedBy { get; init; }
        public required string? Username { get; init; }
        public required string? Usericon { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required string? Summary { get; init; }
        public required bool Sensitive { get; init; }
        public required string? Name { get; init; }
        public required string? HtmlContent { get; init; }

        public required IReadOnlyCollection<ReplyModel> Replies { get; init; }
    }
}
