Imports System.Threading
Imports Azure.Search.Documents.Models
Imports Pandacap.Database
Imports Pandacap.VectorSearch.Models

Public Interface IVectorSearchIndexClient
    ReadOnly Property VectorSearchEnabled As Boolean

    Function GetResultsAsync(query As String,
                             skip As Integer) As IAsyncEnumerable(Of SearchResult(Of EmbeddedPost))

    Function IndexAllAsync(posts As IAsyncEnumerable(Of Post),
                           cancellationToken As CancellationToken) As Task

    Function DeletePostAsync(id As Guid,
                             cancellationToken As CancellationToken) As Task
End Interface
