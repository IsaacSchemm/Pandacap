Imports System.Threading

Public Interface IATProtoHandleLookupClient
    Function FindDIDAsync(handle As String,
                          cancellationToken As CancellationToken) As Task(Of String)
End Interface
