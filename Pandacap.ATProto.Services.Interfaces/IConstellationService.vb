Imports Pandacap.ATProto.Models

Public Interface IConstellationService
    Function GetLinksAsync(target As String,
                           collection As String,
                           path As String) As IAsyncEnumerable(Of ATProtoRefUri)
End Interface
