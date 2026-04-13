Imports System.Threading

Public Interface IRemoteActivityPubFavoritesHandler
    Function AddFavoriteAsync(objectId As String,
                              cancellationToken As CancellationToken) As Task

    Function RemoveFavoritesAsync(objectIds As IEnumerable(Of String),
                                  cancellationToken As CancellationToken) As Task
End Interface
