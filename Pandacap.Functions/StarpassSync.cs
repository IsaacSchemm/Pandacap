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
            await starpassAgent.RefreshAllAsync(newPostLimit: 1);
        }
    }
}
