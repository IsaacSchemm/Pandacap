Imports Pandacap.UI.Elements

Public Interface ICompositeInboxProvider
    Function GetAllInboxPostsAsync() As IAsyncEnumerable(Of IInboxPost)
End Interface
