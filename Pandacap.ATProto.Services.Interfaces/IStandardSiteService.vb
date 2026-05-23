Imports System.Threading
Imports Pandacap.ATProto.Models

Public Interface IStandardSiteService
    Function GetNewestDocumentsAsync(pds As String,
                                     did As String) As IAsyncEnumerable(Of ATProtoRecord(Of StandardSiteDocument))

    Function GetDocumentAsync(pds As String,
                              did As String,
                              recordKey As String,
                              cancellationToken As CancellationToken) As Task(Of ATProtoRecord(Of StandardSiteDocument))

    Function GetPublicationAsync(pds As String,
                                 did As String,
                                 recordKey As String,
                                 cancellationToken As CancellationToken) As Task(Of ATProtoRecord(Of StandardSitePublication))
End Interface
