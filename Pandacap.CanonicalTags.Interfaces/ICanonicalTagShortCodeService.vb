Imports System.Threading
Imports Pandacap.Database

Public Interface ICanonicalTagShortCodeService
    Function GetShortCodesForAttachedCanonicalTagsAsync(post As Post) As IAsyncEnumerable(Of String)

    Function ApplyCanonicalTagsUsingShortCodesAsync(postId As Guid,
                                                    shortCodes As IEnumerable(Of String),
                                                    cancellationToken As CancellationToken) As Task
End Interface
