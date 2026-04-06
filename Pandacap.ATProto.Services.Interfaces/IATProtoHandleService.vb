Imports System.Threading

Public Interface IATProtoHandleService
    Function FindDIDAsync(handle As String,
                          cancellationToken As CancellationToken) As Task(Of String)
End Interface
