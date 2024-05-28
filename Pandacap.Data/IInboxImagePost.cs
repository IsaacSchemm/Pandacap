namespace Pandacap.Data
{
    public interface IInboxImagePost : IInboxPost
    {
        IEnumerable<IInboxImage> Images { get; }
    }
}
