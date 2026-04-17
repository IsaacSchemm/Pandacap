Imports System.Threading
Imports Pandacap.ATProto.Models

Public Interface IDIDResolver
    Function ResolveAsync(did As String,
                          cancellationToken As CancellationToken) As Task(Of DIDDocument)
End Interface
