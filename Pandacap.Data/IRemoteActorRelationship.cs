namespace Pandacap.Data
{
    public interface IRemoteActorRelationship
    {
        string ActorId { get; }
        bool Pending { get; }
    }
}
