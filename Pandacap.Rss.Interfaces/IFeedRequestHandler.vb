Imports System.Net.Http
Imports System.Threading

Public Interface IFeedRequestHandler
    Function GetAsync(uri As String,
                      cancellationToken As CancellationToken) As Task(Of HttpResponseMessage)
End Interface
