namespace Pandacap.Data
{
    public interface IRemoteActorRelationship
    {
        string ActorId { get; }
        string? PreferredUsername { get; }
        string? IconUrl { get; }
        bool Pending { get; }
    }
}
