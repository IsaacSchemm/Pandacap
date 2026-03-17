using Pandacap.ActivityPub.Inbound;
using Pandacap.Resolvers;

namespace Pandacap.HighLevel.Resolvers
{
    internal class WebFingerResolver(
        WebFingerService webFingerService) : IResolver
    {
        public async Task<ResolverResult> ResolveAsync(
            string input,
            CancellationToken cancellationToken)
        {
            var split = input.Split('@');

            switch (split)
            {
                case ["", var handle, var hostname] when handle != "" && hostname != "":
                    var id = await webFingerService.ResolveIdForHandleAsync(input);
                    return ResolverResult.NewActivityPubActor(id);

                default:
                    return ResolverResult.None;
            }
        }
    }
}
