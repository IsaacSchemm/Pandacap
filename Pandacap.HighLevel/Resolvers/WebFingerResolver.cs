using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.Resolvers;

namespace Pandacap.HighLevel.Resolvers
{
    internal class WebFingerResolver(
        IWebFingerService webFingerService) : IResolver
    {
        public async Task<ResolverResult> ResolveAsync(
            string input,
            CancellationToken cancellationToken)
        {
            var split = input.Split('@');

            try
            {
                switch (split)
                {
                    case ["", var handle, var hostname] when handle != "" && hostname != "":
                        var id = await webFingerService.ResolveIdForHandleAsync(input, cancellationToken);
                        return ResolverResult.NewActivityPubActor(id);
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }

            return ResolverResult.None;
        }
    }
}
