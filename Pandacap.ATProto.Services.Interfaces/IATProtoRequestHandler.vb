Imports System.Net.Http
Imports System.Threading

Public Interface IATProtoRequestHandler
    Function GetJsonAsync(uri As Uri,
                          cancellationToken As CancellationToken) As Task(Of HttpResponseMessage)
End Interface
