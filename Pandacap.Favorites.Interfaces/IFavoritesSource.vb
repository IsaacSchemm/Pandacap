Imports System.Threading

Public Interface IFavoritesSource
    Function ImportFavoritesAsync(cancellationToken As CancellationToken) As Task
End Interface
