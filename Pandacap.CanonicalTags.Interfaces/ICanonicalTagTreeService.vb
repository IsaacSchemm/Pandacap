Public Interface ICanonicalTagTreeService
    Function GetAllTagsAsync() As IAsyncEnumerable(Of ICanonicalTagTreeDisplayNode)
End Interface
