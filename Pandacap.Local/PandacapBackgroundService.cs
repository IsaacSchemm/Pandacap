namespace Pandacap.Local
{
    public abstract class PandacapBackgroundService : BackgroundService
    {
        protected abstract TimeSpan InitialDelay { get; }
        protected abstract TimeSpan Period { get; }

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Delay(InitialDelay, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.WhenAll(
                        RunAsync(stoppingToken),
                        Delay(Period, stoppingToken));
                }
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }

        protected abstract Task RunAsync(CancellationToken cancellationToken);

        private async Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{GetType().Name}: Next run in {timeSpan}");
            await Task.Delay(timeSpan, cancellationToken);
        }
    }
}
