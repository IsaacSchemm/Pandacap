using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class StarpassSync(
        StarpassAgent starpassAgent)
    {
        [Function("StarpassSync")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            List<Exception> exceptions = [];

            async Task c(Task t)
            {
                try
                {
                    await t;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            await c(starpassAgent.RefreshAllAsync(
                newPostLimit: 1));

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
