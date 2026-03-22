Imports System.Threading

Public Interface IWebFingerService
    Function ResolveIdForHandleAsync(resource As String,
                                     cancellationToken As CancellationToken) As Task(Of String)
End Interface
