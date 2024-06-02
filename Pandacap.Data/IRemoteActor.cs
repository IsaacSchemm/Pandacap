namespace Pandacap.Data
{
    public interface IRemoteActorRelationship
    {
        string ActorId { get; }
        DateTimeOffset AddedAt { get; }
        bool Pending { get; }
    }
}
