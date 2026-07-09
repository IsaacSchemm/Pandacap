namespace Pandacap.Local
{
    public abstract class PandacapBackgroundService : BackgroundService
    {
        private static readonly SemaphoreSlim _flag = new(1, 1);

        protected abstract TimeSpan InitialDelay { get; }
        protected abstract TimeSpan Period { get; }

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Console.WriteLine($"{GetType().Name}: Delaying for {InitialDelay}");

                await Task.Delay(InitialDelay, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delay = Task.Delay(Period, stoppingToken);

                    await RunWithLockAsync(stoppingToken);

                    await delay;
                }
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }

        private async Task RunWithLockAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"{GetType().Name}: Waiting for flag");

            await _flag.WaitAsync(cancellationToken);

            try
            {
                Console.WriteLine($"{GetType().Name}: Running now, will be run again in {Period}");
                await RunAsync(cancellationToken);
            }
            finally
            {
                _flag.Release();
            }
        }

        protected abstract Task RunAsync(CancellationToken cancellationToken);
    }
}
