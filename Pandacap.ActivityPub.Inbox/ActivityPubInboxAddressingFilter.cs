using Pandacap.ActivityPub.Models.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Models;

namespace Pandacap.ActivityPub.Inbox
{
    internal class ActivityPubInboxAddressingFilter
    {
        internal static bool IsIncludedInInbox(
            RemotePost post,
            IActivityPubFollow relationship)
        {
            var isPublic = post.Recipients.Contains(RemoteAddressee.PublicCollection);
            var addressesSpecificActors = post.Recipients.Any(r => r.IsActor);
            var addressesMe = post.Recipients.Any(r => r.Id == Static.ActivityPubHostInformation.ActorId);

            return addressesMe || (isPublic && !addressesSpecificActors);
        }
    }
}
