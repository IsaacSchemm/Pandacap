Imports System.Threading
Imports Pandacap.ActivityPub.RemoteObjects.Models

Public Interface IRemoteActivityPubInboxHandler
    Function AddRemotePostAsync(sendingActor As RemoteActor,
                                remotePost As RemotePost,
                                cancellationToken As CancellationToken) As Task

    Function AddRemoteAnnouncementAsync(announcingActor As RemoteActor,
                                        announceActivityId As String,
                                        objectId As String,
                                        cancellationToken As CancellationToken) As Task
End Interface
