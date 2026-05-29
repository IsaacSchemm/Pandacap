Imports System.Threading

Public Interface ICanonicalTagShortCodeService
    Function GetShortCodesForAttachedCanonicalTagsAsync(postId As Guid) As IAsyncEnumerable(Of String)

    Function ApplyCanonicalTagsUsingShortCodesAsync(postId As Guid,
                                                    shortCodes As IEnumerable(Of String),
                                                    cancellationToken As CancellationToken) As Task
End Interface
