Imports System.Threading
Imports Microsoft.FSharp.Collections
Imports Pandacap.Lemmy.Models

Public Interface ILemmyClient
    Function GetCommunityAsync(host As String,
                               name As String,
                               cancellationToken As CancellationToken) As Task(Of Community)

    Function GetPostAsync(host As String,
                          id As Integer,
                          cancellationToken As CancellationToken) As Task(Of (PostView, Community))

    'Function GetPostsAsync(host As String,
    '                       community_id As Integer,
    '                       sort As GetPostsSort,
    '                       page As Integer,
    '                       limit As Integer,
    '                       cancellationToken As CancellationToken) As Task(Of FSharpList(Of PostView))

    Function GetPostsAsync(host As String,
                           community_id As Integer,
                           sort As GetPostsSort,
                           Optional start_page As Integer = 1) As IAsyncEnumerable(Of PostView)

    Function GetCommentsAsync(host As String,
                              post_id As Integer,
                              sort As GetCommentsSort) As IAsyncEnumerable(Of CommentObject)

    Function Restructure(comments As IEnumerable(Of CommentObject)) As FSharpList(Of CommentBranch)
End Interface
