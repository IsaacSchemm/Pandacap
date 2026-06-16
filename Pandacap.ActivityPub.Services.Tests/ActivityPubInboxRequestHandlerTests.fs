namespace Pandacap.ActivityPub.Services.Tests

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.VisualStudio.TestTools.UnitTesting
open Newtonsoft.Json.Linq
open JsonLD.Core
open Moq
open Pandacap.ActivityPub.Inbox.Interfaces
open Pandacap.ActivityPub.RemoteObjects.Models
open Pandacap.ActivityPub.RemoteObjects.Interfaces
open Pandacap.ActivityPub.Services.Interfaces
open Pandacap.ActivityPub.Services

module ActivityPubInboxRequestHandlerTests =
    type ITestConfiguration =
        abstract member WhereJsonIs: json: string -> IJsonConfigured

    and IJsonConfigured =
        abstract member WhereMyActorIdIs: id: string -> IMyActorIdConfigured

    and IMyActorIdConfigured =
        abstract member WhereRemoteActorIs: actorId: string -> IRemoteActorConfigured
        abstract member WhereRemoteActorHasWrongId: unit -> IRemoteActorConfigured

    and IRemoteActorConfigured =
        abstract member ShouldRecordFollow: activityId: string -> IExpectationsConfigured
        abstract member ShouldEraseFollow: unit -> IExpectationsConfigured
        abstract member ShouldMarkFollowerAs: accepted: bool * activityId: string -> IExpectationsConfigured
        abstract member ShouldRecordAnnouncement: activityId: string * subjectIds: string list -> IExpectationsConfigured
        abstract member ShouldRecordInteraction: activityId: string * activityType: string * subjectIds: string list -> IExpectationsConfigured
        abstract member ShouldEraseAnnouncement: activityId: string -> IExpectationsConfigured
        abstract member ShouldEraseInteraction: activityId: string -> IExpectationsConfigured
        abstract member MayEraseAnnouncement: unit -> IExpectationsConfigured
        abstract member MayEraseInteraction: unit -> IExpectationsConfigured
        abstract member ShouldRecordPost: objectId: string -> IExpectationsConfigured
        abstract member ShouldUpdateActor: unit -> IExpectationsConfigured
        abstract member WherePostIsKnown: objectId: string -> IKnownPostsConfigured
        abstract member ShouldDoNothingElse: unit -> IExpectationsConfigured

    and IKnownPostsConfigured =
        abstract member ShouldUpdatePost: objectId: string -> IExpectationsConfigured
        abstract member ShouldErasePost: objectId: string -> IExpectationsConfigured

    and IExpectationsConfigured =
        abstract member And: IRemoteActorConfigured
        abstract member RunTestAsync: unit -> Task

    type TestBuilder private () =
        let exampleActor = {
            Type = "Person"
            Id = "https://www.example.net/remote-user"
            Inbox = "https://www.example.net/remote-user/user-inbox"
            SharedInbox = "https://www.example.net/shared-inbox"
            PreferredUsername = "remote-user"
            Name = "Remote User"
            Summary = "A test user"
            Url = "https://www.example.net/remote-user/index.html"
            IconUrl = "https://www.example.net/remote-user/icon.png"
            KeyId = "https://www.example.net/remote-user#key"
            KeyPem = "KEY_HERE"
        }

        let exampleRemotePost = {
            Id = ""
            AttributedTo = { exampleActor with Id = "???" }
            To = []
            Cc = []
            InReplyTo = []
            Type = ""
            PostedAt = DateTimeOffset.MinValue
            Sensitive = false
            Name = ""
            Summary = ""
            SanitizedContent = ""
            Url = ""
            Audience = ""
            Attachments = []
            IsBridgyFed = false
        }

        let mutable expansionObj = None
        let mutable myActorId = None
        let mutable remoteActor = None

        let cancellationToken =
            let cts = new CancellationTokenSource()
            cts.Token

        let activityPubRemoteActorServiceMock = new Mock<IActivityPubRemoteActorService>(MockBehavior.Strict)
        let activityPubRemotePostServiceMock = new Mock<IActivityPubRemotePostService>(MockBehavior.Strict)

        let activityPubInboxActionHandlerMock =
            let mock = new Mock<IActivityPubInboxActionHandler>(MockBehavior.Strict)

            mock
                .Setup(fun svc -> svc.IsPostKnownAsync(
                    It.IsAny(),
                    cancellationToken))
                .ReturnsAsync(false)
                .Verifiable(Times.AtMostOnce)
                |> ignore

            mock

        static member BeginConfiguration() = new TestBuilder() :> ITestConfiguration

        interface ITestConfiguration with
            member this.WhereJsonIs(json) =
                let jObject = JObject.Parse(json)
                jObject["@context"] <- new JArray(
                    new JValue("https://www.w3.org/ns/activitystreams"),
                    new JValue("https://w3id.org/security/v1"))

                expansionObj <- JsonLdProcessor.Expand(jObject) |> Seq.exactlyOne |> Some

                this

        interface IJsonConfigured with
            member this.WhereMyActorIdIs(id) =
                myActorId <- Some id
                this

        interface IMyActorIdConfigured with
            member this.WhereRemoteActorHasWrongId() =
                let actor = { exampleActor with Id = $"{Guid.NewGuid()}" }

                remoteActor <- Some actor

                activityPubRemoteActorServiceMock
                    .Setup(fun svc -> svc.FetchActorAsync(
                        It.IsAny(),
                        cancellationToken))
                    .ReturnsAsync(actor)
                    |> ignore

                this

            member this.WhereRemoteActorIs(actorId) =
                let actor = { exampleActor with Id = actorId }

                remoteActor <- Some actor

                activityPubRemoteActorServiceMock
                    .Setup(fun svc -> svc.FetchActorAsync(
                        actor.Id,
                        cancellationToken))
                    .ReturnsAsync(actor)
                    |> ignore

                this

        interface IRemoteActorConfigured with
            member this.MayEraseAnnouncement() =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.EraseAnnouncementAsync(
                        It.IsAny(),
                        remoteActor.Value.Id,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.AtMostOnce)
                    |> ignore

                this

            member this.MayEraseInteraction() =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.EraseInteractionAsync(
                        It.IsAny(),
                        remoteActor.Value.Id,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.AtMostOnce)
                    |> ignore

                this

            member this.ShouldDoNothingElse() = this

            member this.ShouldRecordAnnouncement(activityId, subjectIds) =
                activityPubRemotePostServiceMock
                    .Setup(fun svc -> svc.GetAnnouncementSubjectIds(expansionObj.Value))
                    .Returns(subjectIds)
                    .Verifiable(Times.AtMostOnce)
                    |> ignore

                for interactedWithId in subjectIds do
                    activityPubInboxActionHandlerMock
                        .Setup(fun handler -> handler.RecordAnnouncementAsync(
                            remoteActor.Value,
                            activityId,
                            interactedWithId,
                            cancellationToken))
                        .Returns(Task.CompletedTask)
                        .Verifiable(Times.Once)
                        |> ignore

                this

            member this.ShouldRecordInteraction(activityId, activityType, subjectIds) =
                activityPubRemotePostServiceMock
                    .Setup(fun svc -> svc.GetAnnouncementSubjectIds(expansionObj.Value))
                    .Returns(subjectIds)
                    .Verifiable(Times.AtMostOnce)
                    |> ignore

                for interactedWithId in subjectIds do
                    activityPubInboxActionHandlerMock
                        .Setup(fun handler -> handler.RecordInteractionAsync(
                            activityId,
                            interactedWithId,
                            remoteActor.Value.Id,
                            activityType,
                            cancellationToken))
                        .Returns(Task.CompletedTask)
                        .Verifiable(Times.Once)
                        |> ignore

                this

            member this.ShouldEraseAnnouncement(activityId) =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.EraseAnnouncementAsync(
                        activityId,
                        remoteActor.Value.Id,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

            member this.ShouldEraseFollow() =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.EraseFollowAsync(
                        remoteActor.Value.Id,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

            member this.ShouldEraseInteraction(activityId) =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.EraseInteractionAsync(
                        activityId,
                        remoteActor.Value.Id,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

            member this.ShouldMarkFollowerAs(accepted, activityId) =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.MarkFollowerAsync(
                        activityId,
                        remoteActor.Value.Id,
                        accepted,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

            member this.ShouldRecordFollow(activityId) =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.RecordFollowAsync(
                        activityId,
                        remoteActor.Value,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

            member this.ShouldRecordPost(objectId) =
                let post = { exampleRemotePost with Id = objectId }

                match expansionObj.Value["https://www.w3.org/ns/activitystreams#object"] with
                | null -> ()
                | token ->
                    activityPubRemotePostServiceMock
                        .Setup(fun svc -> svc.ParseExpandedObjectAsync(
                            token[0],
                            cancellationToken))
                        .ReturnsAsync(post)
                        |> ignore

                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.RecordPostAsync(
                        remoteActor.Value,
                        post,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

            member this.ShouldUpdateActor() =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.UpdateRemoteActorAsync(
                        remoteActor.Value,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

            member this.WherePostIsKnown(objectId) =
                activityPubInboxActionHandlerMock
                    .Setup(fun svc -> svc.IsPostKnownAsync(
                        objectId,
                        cancellationToken))
                    .ReturnsAsync(true)
                    |> ignore

                this

        interface IKnownPostsConfigured with
            member this.ShouldUpdatePost(objectId) =
                let post = { exampleRemotePost with Id = objectId }

                match expansionObj.Value["https://www.w3.org/ns/activitystreams#object"] with
                | null -> ()
                | token ->
                    activityPubRemotePostServiceMock
                        .Setup(fun svc -> svc.ParseExpandedObjectAsync(
                            token[0],
                            cancellationToken))
                        .ReturnsAsync(post)
                        |> ignore

                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.UpdatePostAsync(
                        remoteActor.Value,
                        post,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

            member this.ShouldErasePost(objectId) =
                activityPubInboxActionHandlerMock
                    .Setup(fun handler -> handler.ErasePostAsync(
                        remoteActor.Value.Id,
                        objectId,
                        cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once)
                    |> ignore

                this

        interface IExpectationsConfigured with
            member this.And = this

            member _.RunTestAsync() = task {
                let handler = new ActivityPubInboxRequestHandler(
                    activityPubRemoteActorServiceMock.Object,
                    activityPubRemotePostServiceMock.Object,
                    activityPubInboxActionHandlerMock.Object) :> IActivityPubInboxRequestHandler

                do! handler.ProcessVerifiedInboxMessageAsync(
                    expansionObj.Value,
                    myActorId.Value,
                    cancellationToken)

                activityPubRemoteActorServiceMock.VerifyAll()
                activityPubRemotePostServiceMock.VerifyAll()
                activityPubInboxActionHandlerMock.VerifyAll()

                activityPubRemoteActorServiceMock.VerifyNoOtherCalls()
                activityPubRemotePostServiceMock.VerifyNoOtherCalls()
                activityPubInboxActionHandlerMock.VerifyNoOtherCalls()
            }

[<TestClass>]
type ActivityPubInboxRequestHandlerTests() =
    [<TestMethod>]
    member _.Follow_RecordsFollow() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity01",
                "type": "Follow",
                "actor": "correct-user",
                "object": "myself"
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("correct-user")
            .ShouldRecordFollow("activity01")
            .RunTestAsync()

    [<TestMethod>]
    member _.Follow_WrongActor_IgnoresFollow() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity01",
                "type": "Follow",
                "actor": "correct-user",
                "object": "myself"
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorHasWrongId()
            .ShouldDoNothingElse()
            .RunTestAsync()

    [<TestMethod>]
    member _.Follow_WrongTarget_IgnoresFollow() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity01",
                "type": "Follow",
                "actor": "correct-user",
                "object": "someone-else"
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("correct-user")
            .ShouldDoNothingElse()
            .RunTestAsync()

    [<TestMethod>]
    member _.Undo_Announce_ErasesInteraction_ErasesAnnouncement() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity77",
                "type": "Undo",
                "actor": "correct-user",
                "object": {
                    "id": "activity01",
                    "type": "Announce",
                    "actor": "correct-user",
                    "object": "post1"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("correct-user")
            .ShouldEraseInteraction("activity01")
            .And
            .ShouldEraseAnnouncement("activity01")
            .RunTestAsync()

    [<TestMethod>]
    member _.Undo_Like_ErasesInteraction() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity77",
                "type": "Undo",
                "actor": "correct-user",
                "object": {
                    "id": "activity01",
                    "type": "Like",
                    "actor": "correct-user",
                    "object": "post1"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("correct-user")
            .ShouldEraseInteraction("activity01")
            .And
            .MayEraseAnnouncement()
            .RunTestAsync()

    [<TestMethod>]
    member _.Undo_Follow_ErasesFollow() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity77",
                "type": "Undo",
                "actor": "correct-user",
                "object": {
                    "id": "activity01",
                    "type": "Follow",
                    "actor": "correct-user",
                    "object": "myself"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("correct-user")
            .ShouldEraseFollow()
            .And
            .MayEraseInteraction()
            .And
            .MayEraseAnnouncement()
            .RunTestAsync()

    [<TestMethod>]
    member _.Undo_Follow_WrongActor_IgnoresUndo() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity77",
                "type": "Undo",
                "actor": "correct-user",
                "object": {
                    "id": "activity01",
                    "type": "Follow",
                    "actor": "correct-user",
                    "object": "myself"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorHasWrongId()
            .ShouldDoNothingElse()
            .RunTestAsync()

    [<TestMethod>]
    member _.Undo_Follow_WrongTarget_IgnoresUndo() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity77",
                "type": "Undo",
                "actor": "correct-user",
                "object": {
                    "id": "activity01",
                    "type": "Follow",
                    "actor": "other-user",
                    "object": "someone-else"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("correct-user")
            .MayEraseAnnouncement()
            .And
            .MayEraseInteraction()
            .And
            .ShouldDoNothingElse()
            .RunTestAsync()

    [<TestMethod>]
    member _.Accept_UpdatesFollow() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity100",
                "type": "Accept",
                "actor": "remote-user",
                "object": { "id": "activity200" }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("remote-user")
            .ShouldMarkFollowerAs(accepted = true, activityId = "activity200")
            .RunTestAsync()

    [<TestMethod>]
    member _.Reject_UpdatesFollow() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity100",
                "type": "Reject",
                "actor": "remote-user",
                "object": { "id": "activity200" }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("remote-user")
            .ShouldMarkFollowerAs(accepted = false, activityId = "activity200")
            .RunTestAsync()

    [<TestMethod>]
    member _.Announce_RecordsAnnouncement_RecordsInteraction() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity100",
                "type": "Announce",
                "actor": "https://remote.example.com/remote-user",
                "object": { "id": "https://www.example.com/my-object" }
            }""")
            .WhereMyActorIdIs("https://www.example.com/myself")
            .WhereRemoteActorIs("https://remote.example.com/remote-user")
            .ShouldRecordAnnouncement("activity100", ["https://www.example.com/my-object"])
            .And
            .ShouldRecordInteraction("activity100", "https://www.w3.org/ns/activitystreams#Announce", ["https://www.example.com/my-object"])
            .RunTestAsync()

    [<TestMethod>]
    member _.Announce_RefersToRemoteObject_RecordsAnnouncementOnly() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity100",
                "type": "Announce",
                "actor": "https://remote.example.com/remote-user",
                "object": { "id": "https://other.example.com/remote-object" }
            }""")
            .WhereMyActorIdIs("https://www.example.com/myself")
            .WhereRemoteActorIs("https://remote.example.com/remote-user")
            .ShouldRecordAnnouncement("activity100", ["https://other.example.com/remote-object"])
            .RunTestAsync()

    [<TestMethod>]
    [<DataRow("https://www.w3.org/ns/activitystreams#Like")>]
    [<DataRow("https://www.w3.org/ns/activitystreams#Dislike")>]
    [<DataRow("https://ns.mia.jetzt/as#Bite")>]
    member _.OtherInteraction_RecordsInteraction(activityType: string) =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs(
                Newtonsoft.Json.JsonConvert.SerializeObject({|
                    id = "activity300"
                    ``type`` = activityType.Replace("https://www.w3.org/ns/activitystreams#", "")
                    actor = "remote-actor"
                    object = ["http://myself.example.com/post1"]
                |}))
            .WhereMyActorIdIs("http://myself.example.com/me")
            .WhereRemoteActorIs("remote-actor")
            .ShouldRecordInteraction("activity300", activityType, ["http://myself.example.com/post1"])
            .RunTestAsync()

    [<TestMethod>]
    member _.Create_RecordsPost() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity400",
                "type": "Create",
                "actor": "remote-user",
                "object": {
                    "id": "post400"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("remote-user")
            .ShouldRecordPost("post400")
            .RunTestAsync()

    [<TestMethod>]
    member _.Update_UpdatesRemoteActor() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity400",
                "type": "Update",
                "actor": "actor100",
                "object": {
                    "id": "actor100",
                    "type": "Person"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("actor100")
            .ShouldUpdateActor()
            .RunTestAsync()

    [<TestMethod>]
    member _.Update_DoesNotUpdateRemoteActor() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity400",
                "type": "Update",
                "actor": "actor200",
                "object": {
                    "id": "actor100",
                    "type": "Person"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("actor200")
            .ShouldDoNothingElse()
            .RunTestAsync()

    [<TestMethod>]
    member _.Update_UpdatesPost() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity400",
                "type": "Update",
                "actor": "actor100",
                "object": {
                    "id": "post400"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("actor100")
            .WherePostIsKnown("post400")
            .ShouldUpdatePost("post400")
            .RunTestAsync()

    [<TestMethod>]
    member _.Update_DoesNotUpdateUnknownPost() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity400",
                "type": "Update",
                "actor": "actor100",
                "object": {
                    "id": "post400"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("actor100")
            .ShouldDoNothingElse()
            .RunTestAsync()

    [<TestMethod>]
    member _.Delete_DeletesPost() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity400",
                "type": "Delete",
                "actor": "actor100",
                "object": {
                    "id": "post400"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("actor100")
            .WherePostIsKnown("post400")
            .ShouldErasePost("post400")
            .RunTestAsync()

    [<TestMethod>]
    member _.Delete_DoesNotDeleteUnknownPost() =
        ActivityPubInboxRequestHandlerTests
            .TestBuilder
            .BeginConfiguration()
            .WhereJsonIs("""{
                "id": "activity400",
                "type": "Delete",
                "actor": "actor100",
                "object": {
                    "id": "post400"
                }
            }""")
            .WhereMyActorIdIs("myself")
            .WhereRemoteActorIs("actor100")
            .ShouldDoNothingElse()
            .RunTestAsync()
