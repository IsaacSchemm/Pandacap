namespace Pandacap.ActivityPub.Models.Interfaces
{
    /// <summary>
    /// A portion of an ActivityPub ID that may be relative to the root domain of the Pandacap instance.
    /// </summary>
    public interface IActivityPubPandacapRelativePath
    {
        /// <summary>
        /// The relative path (e.g. "/UserPosts/xxx") or an absolute URL (e.g. "https://www.example.com/posts/123").
        /// </summary>
        string RelativePath { get; }
    }
}
