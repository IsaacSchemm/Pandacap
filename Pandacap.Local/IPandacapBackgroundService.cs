namespace Pandacap.Local
{
    public interface IPandacapBackgroundService
    {
        Task RunAsync(CancellationToken cancellationToken = default);
    }
}
