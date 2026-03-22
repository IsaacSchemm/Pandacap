namespace Pandacap.ActivityPub.Models.Interfaces
{
    public interface IActivityPubAddressing
    {
        /// <summary>
        /// The ActivityPub object ID of the post that this post is in reply to, if any.
        /// </summary>
        string? InReplyTo { get; }

        /// <summary>
        /// The URLs / IDs for ActivityPub's "to" field.
        /// Relative URLs are allowed (e.g. for the Pandacap admin's followers collection) and will be interpreted as being relative to the root of the Pandacap instance.
        /// </summary>
        IEnumerable<IActivityPubPandacapRelativePath> To { get; }

        /// <summary>
        /// The URLs / IDs for ActivityPub's "cc" field.
        /// Relative URLs are allowed (e.g. for the Pandacap admin's followers collection) and will be interpreted as being relative to the root of the Pandacap instance.
        /// </summary>
        IEnumerable<IActivityPubPandacapRelativePath> Cc { get; }

        /// <summary>
        /// The URL / ID for the "audience" field, if any.
        /// </summary>
        string? Audience { get; }
    }
}
