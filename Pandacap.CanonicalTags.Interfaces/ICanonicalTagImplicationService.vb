Imports Pandacap.Database

Public Interface ICanonicalTagImplicationService
    Function GetImplicitTagsAsync(post As Post) As IAsyncEnumerable(Of Guid)
End Interface
