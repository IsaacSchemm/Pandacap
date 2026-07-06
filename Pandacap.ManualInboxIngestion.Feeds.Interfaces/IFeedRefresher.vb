Imports System.Threading

Public Interface IFeedRefresher
    Function AddFeedAsync(url As String,
                          cancellationToken As CancellationToken) As Task

    Function RefreshFeedAsync(id As Guid,
                              cancellationToken As CancellationToken) As Task
End Interface
