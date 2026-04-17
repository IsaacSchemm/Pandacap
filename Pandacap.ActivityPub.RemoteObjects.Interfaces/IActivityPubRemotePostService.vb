Imports System.Threading
Imports Microsoft.FSharp.Collections
Imports Newtonsoft.Json.Linq
Imports Pandacap.ActivityPub.RemoteObjects.Models

Public Interface IActivityPubRemotePostService
    Function GetAnnouncementSubjectIds(expandedAnnounceObject As JToken) As FSharpList(Of String)

    Function GetAttachments(obj As JToken) As FSharpList(Of RemoteAttachment)

    Function ParseExpandedObjectAsync(obj As JToken,
                                      cancellationToken As CancellationToken) As Task(Of RemotePost)

    Function FetchPostAsync(url As String,
                            cancellationToken As CancellationToken) As Task(Of RemotePost)
End Interface
