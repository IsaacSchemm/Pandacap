Imports System.Threading
Imports Microsoft.AspNetCore.Http

Public Interface IActivityAuthenticator
    ''' <summary>
    ''' Extracts the key ID from the Signature header and fetches the corresponding key.
    ''' </summary>
    ''' <param name="request">The HTTP request</param>
    ''' <param name="cancellationToken">A cancellation token</param>
    ''' <returns></returns>
    Function AcquireKeysAsync(request As HttpRequest,
                              Optional cancellationToken As CancellationToken = Nothing) As IAsyncEnumerable(Of IKeyWithOwner)
End Interface
