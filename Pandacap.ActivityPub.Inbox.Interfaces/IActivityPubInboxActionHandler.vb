Imports System.Threading
Imports Pandacap.ActivityPub.RemoteObjects.Models

Public Interface IActivityPubInboxActionHandler
    Function UpdateRemoteActorAsync(actor As RemoteActor,
                                    cancellationToken As CancellationToken) As Task

    Function MarkFollowerAsync(followActivityId As String,
                               actorId As String,
                               accepted As Boolean,
                               cancellationToken As CancellationToken) As Task

    Function RecordFollowAsync(followActivityId As String,
                               actor As RemoteActor,
                               cancellationToken As CancellationToken) As Task

    Function EraseFollowAsync(actorId As String,
                              cancellationToken As CancellationToken) As Task

    Function RecordInteractionAsync(activityId As String,
                                    interactedWithPostId As String,
                                    actorId As String,
                                    activityType As String,
                                    cancellationToken As CancellationToken) As Task

    Function EraseInteractionAsync(activityId As String,
                                   actorId As String,
                                   cancellationToken As CancellationToken) As Task

    Function RecordAnnouncementAsync(announcingActor As RemoteActor,
                                     announceActivityId As String,
                                     objectId As String,
                                     cancellationToken As CancellationToken) As Task

    Function EraseAnnouncementAsync(announceActivityId As String,
                                    actorId As String,
                                    cancellationToken As CancellationToken) As Task

    Function RecordPostAsync(sendingActor As RemoteActor,
                             post As RemotePost,
                             cancellationToken As CancellationToken) As Task

    Function IsPostKnownAsync(postId As String,
                              cancellationToken As CancellationToken) As Task(Of Boolean)

    Function UpdatePostAsync(sendingActor As RemoteActor,
                             post As RemotePost,
                             cancellationToken As CancellationToken) As Task

    Function ErasePostAsync(sendingActorId As String,
                            postId As String,
                            cancellationToken As CancellationToken) As Task
End Interface
