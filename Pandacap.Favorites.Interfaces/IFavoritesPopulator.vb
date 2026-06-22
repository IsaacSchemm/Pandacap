Imports System.Threading

Public Interface IFavoritesPopulator
    Function PopulateFavoritesAsync(Optional cancellationToken As CancellationToken = Nothing) As Task
End Interface
