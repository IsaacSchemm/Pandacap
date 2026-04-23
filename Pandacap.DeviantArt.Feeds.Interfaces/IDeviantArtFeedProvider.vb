Imports Pandacap.UI.Elements

Public Interface IDeviantArtFeedProvider
    Function GetHomeFeedAsync() As IAsyncEnumerable(Of IPost)
End Interface
