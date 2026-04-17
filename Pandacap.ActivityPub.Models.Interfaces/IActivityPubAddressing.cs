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
        /// </summary>
        IEnumerable<string> To { get; }

        /// <summary>
        /// The URLs / IDs for ActivityPub's "cc" field.
        /// </summary>
        IEnumerable<string> Cc { get; }

        /// <summary>
        /// The URL / ID for the "audience" field, if any.
        /// </summary>
        string? Audience { get; }
    }
}
