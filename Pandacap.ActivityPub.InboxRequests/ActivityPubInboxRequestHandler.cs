using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.Inbox.Interfaces;
using Pandacap.ActivityPub.InboxRequests.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;

namespace Pandacap.ActivityPub.InboxRequests
{
    public class ActivityPubInboxRequestHandler(
        IActivityPubRemoteActorService activityPubRemoteActorService,
        IActivityPubRemotePostService activityPubRemotePostService,
        IActivityPubInboxActionHandler activityPubInboxActionHandler) : IActivityPubInboxRequestHandler
    {
        private static readonly IEnumerable<JToken> Empty = [];

        public async Task ProcessVerifiedInboxMessageAsync(
            JToken expansionObj,
            string myActorId,
            CancellationToken cancellationToken)
        {
            // Find out which ActivityPub actor they say they are
            string actorId = expansionObj["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;

            // Grab that actor's information
            var actor = await activityPubRemoteActorService.FetchActorAsync(actorId, cancellationToken);

            string type = expansionObj["@type"]![0]!.Value<string>()!;

            if (type == "https://www.w3.org/ns/activitystreams#Follow")
            {
                string activityId = expansionObj["@id"]!.Value<string>()!;

                string fActor = expansionObj["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;
                string fObject = expansionObj["https://www.w3.org/ns/activitystreams#object"]![0]!["@id"]!.Value<string>()!;

                if (fActor == actor.Id && fObject == myActorId)
                    await activityPubInboxActionHandler.RecordFollowAsync(activityId, actor, cancellationToken);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                foreach (var objectToUndo in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    if ((objectToUndo["@type"] ?? Empty).Any(token => token.Value<string>() == "https://www.w3.org/ns/activitystreams#Follow"))
                    {
                        string fActor = objectToUndo["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;
                        string fObject = objectToUndo["https://www.w3.org/ns/activitystreams#object"]![0]!["@id"]!.Value<string>()!;
                        if (fActor == actor.Id && fObject == myActorId)
                        {
                            await activityPubInboxActionHandler.EraseFollowAsync(fActor, cancellationToken);
                        }
                    }

                    string id = objectToUndo["@id"]!.Value<string>()!;

                    await activityPubInboxActionHandler.EraseInteractionAsync(id, cancellationToken);
                    await activityPubInboxActionHandler.EraseAnnouncementAsync(id, cancellationToken);
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Accept")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string followId = obj["@id"]!.Value<string>()!;
                    await activityPubInboxActionHandler.MarkFollowerAsync(actor.Id, followId, accepted: true, cancellationToken);
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Reject")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string followId = obj["@id"]!.Value<string>()!;
                    await activityPubInboxActionHandler.MarkFollowerAsync(actor.Id, followId, accepted: false, cancellationToken);
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Like"
                || type == "https://www.w3.org/ns/activitystreams#Dislike"
                || type == "https://www.w3.org/ns/activitystreams#Flag"
                || type == "https://www.w3.org/ns/activitystreams#Listen"
                || type == "https://www.w3.org/ns/activitystreams#Read"
                || type == "https://www.w3.org/ns/activitystreams#View"
                || type == "https://www.w3.org/ns/activitystreams#Announce"
                || type == "https://ns.mia.jetzt/as#Bite")
            {
                foreach (string interactedWithId in activityPubRemotePostService.GetAnnouncementSubjectIds(expansionObj))
                {
                    if (type == "https://www.w3.org/ns/activitystreams#Announce")
                    {
                        await activityPubInboxActionHandler.RecordAnnouncementAsync(
                            actor,
                            expansionObj["@id"]!.Value<string>()!,
                            interactedWithId,
                            cancellationToken);
                    }

                    if (Uri.TryCreate(interactedWithId, UriKind.Absolute, out Uri? uri)
                        && uri != null
                        && Uri.TryCreate(myActorId, UriKind.Absolute, out Uri? me)
                        && me != null
                        && uri.Host == me.Host)
                    {
                        await activityPubInboxActionHandler.RecordInteractionAsync(
                            expansionObj["@id"]!.Value<string>()!,
                            interactedWithId,
                            actor.Id,
                            type,
                            cancellationToken);
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    var remotePost = await activityPubRemotePostService.ParseExpandedObjectAsync(obj, cancellationToken);
                    await activityPubInboxActionHandler.RecordPostAsync(actor, remotePost, cancellationToken);
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Update")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string postId = obj["@id"]!.Value<string>()!;
                    string postType = obj["@type"]!.Value<string>()!;

                    if (postType == "Person" && postId == actor.Id)
                    {
                        await activityPubInboxActionHandler.UpdateRemoteActorAsync(actor, cancellationToken);
                    }

                    if (await activityPubInboxActionHandler.IsPostKnownAsync(postId, cancellationToken))
                    {
                        var remotePost = await activityPubRemotePostService.ParseExpandedObjectAsync(obj, cancellationToken);
                        await activityPubInboxActionHandler.UpdatePostAsync(actor, remotePost, cancellationToken);
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                foreach (var deletedObject in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string deletedObjectId = deletedObject["@id"]!.Value<string>()!;

                    await activityPubInboxActionHandler.ErasePostAsync(actor.Id, deletedObjectId, cancellationToken);
                }
            }
        }
    }
}
