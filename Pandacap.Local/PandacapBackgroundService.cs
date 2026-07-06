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
                Console.WriteLine($"{GetType().Name}: Delaying for {InitialDelay}");

                await Delay(InitialDelay, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine($"{GetType().Name}: Running now, will be run again in {Period}");

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
            await Task.Delay(timeSpan, cancellationToken);
        }
    }
}
