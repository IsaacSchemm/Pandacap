Imports System.Threading

Public Interface IFavoritesSource
    Function ImportFavoritesAsync(Optional cancellationToken As CancellationToken = Nothing) As Task
End Interface
