Imports System.Threading
Imports Pandacap.ATProto.Models

Public Interface IBlueskyService
    Function GetNewestLikesAsync(pds As String,
                                 did As String) As IAsyncEnumerable(Of ATProtoRecord(Of BlueskyInteraction))

    Function GetNewestPostsAsync(pds As String,
                                 did As String) As IAsyncEnumerable(Of ATProtoRecord(Of BlueskyPost))

    Function GetNewestRepostsAsync(pds As String,
                                   did As String) As IAsyncEnumerable(Of ATProtoRecord(Of BlueskyInteraction))

    Function GetPostAsync(pds As String,
                          did As String,
                          recordKey As String,
                          cancellationToken As CancellationToken) As Task(Of ATProtoRecord(Of BlueskyPost))

    Function GetProfileAsync(pds As String,
                             did As String,
                             cancellationToken As CancellationToken) As Task(Of ATProtoRecord(Of BlueskyProfile))
End Interface
