Imports Pandacap.UI.Elements

Public Interface ICompositeInboxProvider
    Function GetAllInboxPostsAsync(Optional includeDismissed As Boolean = False) As IAsyncEnumerable(Of IInboxPost)
End Interface
