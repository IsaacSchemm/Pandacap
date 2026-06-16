namespace Pandacap.ActivityPub.Services

open System
open Newtonsoft.Json.Linq
open Pandacap.ActivityPub.Inbox.Interfaces
open Pandacap.ActivityPub.RemoteObjects.Interfaces
open Pandacap.ActivityPub.Services.Interfaces

type internal ActivityPubInboxRequestHandler(
    activityPubRemoteActorService: IActivityPubRemoteActorService,
    activityPubRemotePostService: IActivityPubRemotePostService,
    activityPubInboxActionHandler: IActivityPubInboxActionHandler
) =
    interface IActivityPubInboxRequestHandler with
        member this.ProcessVerifiedInboxMessageAsync(expansionObj, myActorId, cancellationToken) = task {
            let expandedActorObject = Seq.exactlyOne expansionObj["https://www.w3.org/ns/activitystreams#actor"]
            let actorId = (expandedActorObject["@id"]).Value<string>()

            let! actor = activityPubRemoteActorService.FetchActorAsync(actorId, cancellationToken)
            if actor.Id = actorId then
                let activityId = (expansionObj["@id"]).Value<string>()
                let activityType = (Seq.exactlyOne expansionObj["@type"]).Value<string>()

                match activityType with
                | "https://www.w3.org/ns/activitystreams#Follow" ->
                    let target = Seq.exactlyOne expansionObj["https://www.w3.org/ns/activitystreams#object"]
                    let targetId = (target["@id"]).Value<string>()

                    if targetId = myActorId then
                        do! activityPubInboxActionHandler.RecordFollowAsync(activityId, actor, cancellationToken)

                | "https://www.w3.org/ns/activitystreams#Undo" ->
                    for object in expansionObj["https://www.w3.org/ns/activitystreams#object"] do
                        let id = object["@id"].Value<string>()

                        do! activityPubInboxActionHandler.EraseAnnouncementAsync(id, actorId, cancellationToken)
                        do! activityPubInboxActionHandler.EraseInteractionAsync(id, actorId, cancellationToken)

                        let types = set (seq {
                            if not (isNull object["@type"]) then
                                for token in object["@type"] do
                                    token.Value<string>()
                        })

                        if types.Contains("https://www.w3.org/ns/activitystreams#Follow") then
                            let source = Seq.exactlyOne object["https://www.w3.org/ns/activitystreams#actor"]
                            let target = Seq.exactlyOne object["https://www.w3.org/ns/activitystreams#object"]

                            let sourceId = (source["@id"]).Value<string>()
                            let targetId = (target["@id"]).Value<string>()

                            if sourceId = actorId && targetId = myActorId then
                                do! activityPubInboxActionHandler.EraseFollowAsync(actorId, cancellationToken)

                | "https://www.w3.org/ns/activitystreams#Accept" ->
                    for object in expansionObj["https://www.w3.org/ns/activitystreams#object"] do
                        let id = object["@id"].Value<string>()
                        do! activityPubInboxActionHandler.MarkFollowerAsync(id, actorId, true, cancellationToken)

                | "https://www.w3.org/ns/activitystreams#Reject" ->
                    for object in expansionObj["https://www.w3.org/ns/activitystreams#object"] do
                        let id = object["@id"].Value<string>()
                        do! activityPubInboxActionHandler.MarkFollowerAsync(id, actorId, false, cancellationToken)

                | "https://www.w3.org/ns/activitystreams#Like"
                | "https://www.w3.org/ns/activitystreams#Dislike"
                | "https://www.w3.org/ns/activitystreams#Flag"
                | "https://www.w3.org/ns/activitystreams#Listen"
                | "https://www.w3.org/ns/activitystreams#Read"
                | "https://www.w3.org/ns/activitystreams#View"
                | "https://www.w3.org/ns/activitystreams#Announce"
                | "https://ns.mia.jetzt/as#Bite" ->
                    let tryParseUri str =
                        match Uri.TryCreate(str, UriKind.Absolute) with
                        | true, uri -> Some uri
                        | false, _ -> None

                    for targetObjectId in activityPubRemotePostService.GetAnnouncementSubjectIds(expansionObj) do
                        match tryParseUri targetObjectId, tryParseUri myActorId with
                        | (Some uri, Some me) when uri.Host = me.Host ->
                            do! activityPubInboxActionHandler.RecordInteractionAsync(activityId, targetObjectId, actorId, activityType, cancellationToken)
                        | _ -> ()

                        if activityType = "https://www.w3.org/ns/activitystreams#Announce" then
                            do! activityPubInboxActionHandler.RecordAnnouncementAsync(actor, activityId, targetObjectId, cancellationToken)

                | "https://www.w3.org/ns/activitystreams#Create" ->
                    for object in expansionObj["https://www.w3.org/ns/activitystreams#object"] do
                        let! remotePost = activityPubRemotePostService.ParseExpandedObjectAsync(object, cancellationToken)
                        do! activityPubInboxActionHandler.RecordPostAsync(actor, remotePost, cancellationToken)

                | "https://www.w3.org/ns/activitystreams#Update" ->
                    for object in expansionObj["https://www.w3.org/ns/activitystreams#object"] do
                        let id = object["@id"].Value<string>()

                        let types = set (seq {
                            if not (isNull object["@type"]) then
                                for token in object["@type"] do
                                    token.Value<string>()
                        })

                        if types.Contains("https://www.w3.org/ns/activitystreams#Person") && id = actorId then
                            do! activityPubInboxActionHandler.UpdateRemoteActorAsync(actor, cancellationToken)

                        let! known = activityPubInboxActionHandler.IsPostKnownAsync(id, cancellationToken)
                        if known then
                            let! remotePost = activityPubRemotePostService.ParseExpandedObjectAsync(object, cancellationToken)
                            do! activityPubInboxActionHandler.UpdatePostAsync(actor, remotePost, cancellationToken)

                | "https://www.w3.org/ns/activitystreams#Delete" ->
                    for object in expansionObj["https://www.w3.org/ns/activitystreams#object"] do
                        let id = object["@id"].Value<string>()

                        let! known = activityPubInboxActionHandler.IsPostKnownAsync(id, cancellationToken)
                        if known then
                            do! activityPubInboxActionHandler.ErasePostAsync(actorId, id, cancellationToken)

                | _ -> ()

            ()
        }
