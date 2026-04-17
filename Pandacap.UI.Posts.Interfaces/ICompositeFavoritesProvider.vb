Imports Pandacap.UI.Elements

Public Interface ICompositeFavoritesProvider
    Function GetAllAsync() As IAsyncEnumerable(Of IFavorite)

    Function GetAllAsync(guids As IEnumerable(Of Guid)) As IAsyncEnumerable(Of IFavorite)
End Interface
