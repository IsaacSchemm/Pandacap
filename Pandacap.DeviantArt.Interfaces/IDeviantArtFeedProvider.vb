Imports DeviantArtFs
Imports Pandacap.UI.Elements

Public Interface IDeviantArtFeedProvider
    Function GetHomeFeedAsync(token As IDeviantArtAccessToken,
                              Optional offset As Integer = 0) As IAsyncEnumerable(Of IPost)
End Interface
