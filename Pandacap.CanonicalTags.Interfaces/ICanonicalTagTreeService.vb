Public Interface ICanonicalTagTreeService
    Function GetAllMediumsAsync() As IAsyncEnumerable(Of ICanonicalTagTreeDisplayNode)
    Function GetAllCharactersAsync() As IAsyncEnumerable(Of ICanonicalTagTreeDisplayNode)
    Function GetAllSettingsAsync() As IAsyncEnumerable(Of ICanonicalTagTreeDisplayNode)
    Function GetAllSpeciesAsync() As IAsyncEnumerable(Of ICanonicalTagTreeDisplayNode)
End Interface
