namespace Pandacap.ActivityPub.Models.Interfaces
{
    /// <summary>
    /// A user who this ActivityPub user is following.
    /// </summary>
    public interface IActivityPubFollow
    {
        string ActorId { get; }
    }
}
