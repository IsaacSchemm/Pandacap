Imports System.Threading

Public Interface IFavoritesPopulator
    Function PopulateFavoritesAsync(cancellationToken As CancellationToken) As Task
End Interface
