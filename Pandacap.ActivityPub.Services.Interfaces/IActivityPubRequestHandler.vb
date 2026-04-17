Imports System.Threading

Public Interface IActivityPubRequestHandler
    Function GetJsonAsync(uri As Uri,
                          cancellationToken As CancellationToken) As Task(Of String)

    Function PostAsync(uri As Uri,
                       json As String,
                       cancellationToken As CancellationToken) As Task
End Interface
