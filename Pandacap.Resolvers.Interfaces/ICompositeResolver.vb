Imports System.Threading
Imports Pandacap.Resolvers.Models

Public Interface ICompositeResolver
    Function ResolveAsync(url As String,
                          cancellationToken As CancellationToken) As Task(Of ResolverResult)
End Interface
