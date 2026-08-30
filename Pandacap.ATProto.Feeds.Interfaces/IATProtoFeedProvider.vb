Imports Pandacap.UI.Elements

Public Interface IATProtoFeedProvider
    Function GetBlueskyPostsAsync(pds As String,
                                  did As String) As IAsyncEnumerable(Of IPost)
End Interface
