Imports System.Threading

Public Interface IATProtoFeedRefresher
    Function AddFeedAsync(did As String,
                          cancellationToken As CancellationToken) As Task

    Function RefreshFeedAsync(did As String,
                              cancellationToken As CancellationToken) As Task
End Interface
