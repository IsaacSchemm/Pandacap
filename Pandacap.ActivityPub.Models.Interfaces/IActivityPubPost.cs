namespace Pandacap.ActivityPub.Models.Interfaces
{
    public interface IActivityPubPost
    {
        /// <summary>
        /// The ActivityPub object ID / URL for this post.
        /// Relative URLs are allowed and will be interpreted as being relative to the root of the Pandacap instance.
        /// </summary>
        IActivityPubPandacapRelativePath ObjectId { get; }

        /// <summary>
        /// The addressing of the post (e.g. To, CC).
        /// </summary>
        IActivityPubAddressing Addressing { get; }

        /// <summary>
        /// The publication date/time of the post.
        /// </summary>
        DateTimeOffset PublishedTime { get; }

        /// <summary>
        /// Whether this post should use the Article object type instead of Note.
        /// </summary>
        bool IsArticle { get; }

        /// <summary>
        /// The title of this post, if any.
        /// </summary>
        string? Title { get; }

        /// <summary>
        /// The HTML body of this post.
        /// </summary>
        string Html { get; }

        /// <summary>
        /// This post's tags, if any.
        /// </summary>
        IEnumerable<string> Tags { get; }

        /// <summary>
        /// Any links attached to this post. Will be included as ActivityPub attachments.
        /// </summary>
        IEnumerable<IActivityPubLink> Links { get; }

        /// <summary>
        /// Any images attached to this post. Will be included as ActivityPub attachments.
        /// </summary>
        IEnumerable<IActivityPubImage> Images { get; }
    }
}
