using Pandacap.ActivityPub.Inbound;
using Pandacap.Resolvers;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel.Resolvers
{
    internal class WebFingerResolver(
        WebFingerService webFingerService) : IResolver
    {
        public async IAsyncEnumerable<ResolverResult> ResolveAsync(
            string input,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var split = input.Split('@');

            switch (split)
            {
                case ["", var handle, var hostname] when handle != "" && hostname != "":
                    var id = await webFingerService.ResolveIdForHandleAsync(input);
                    yield return ResolverResult.NewActivityPubActor(id);

                    break;

                default:
                    break;
            }
        }
    }
}
