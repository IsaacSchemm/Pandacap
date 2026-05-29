Imports Pandacap.CanonicalTags.Models

Public Interface ICanonicalTagTreeService
    Function GetAllTagsAsync() As IAsyncEnumerable(Of CanonicalTagTreeDisplayNode)
End Interface
