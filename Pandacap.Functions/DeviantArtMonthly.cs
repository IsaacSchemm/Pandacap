using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap.Functions
{
    public class DeviantArtMonthly(
        DeviantArtCredentialProvider credentialProvider,
        DeviantArtHandler deviantArtHandler)
    {
        [Function("DeviantArtMonthly")]
        public async Task Run([TimerTrigger("0 0 10 14 * *")] TimerInfo myTimer)
        {
            var scope = DeviantArtImportScope.NewWindow(
                _oldest: DateTimeOffset.MinValue,
                _newest: DateTimeOffset.UtcNow.AddHours(-1));
            await deviantArtHandler.ImportOurGalleryAsync(scope);
            await deviantArtHandler.ImportOurTextPostsAsync(scope);
            await credentialProvider.UpdateAvatarAsync();
        }
    }
}
