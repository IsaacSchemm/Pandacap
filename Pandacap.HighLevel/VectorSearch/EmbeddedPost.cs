namespace Pandacap.HighLevel.VectorSearch
{
    public class EmbeddedPost
    {
        public required Guid Id { get; init; }
        public float[]? ShortText { get; init; }
        public float[]? LongText { get; init; }
        public required DateTimeOffset PublishedTime { get; init; }
    }
}
