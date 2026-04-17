Imports System.Threading

Public Interface IATProtoFeedReader
    Function RefreshFeedAsync(did As String,
                              cancellationToken As CancellationToken) As Task
End Interface
