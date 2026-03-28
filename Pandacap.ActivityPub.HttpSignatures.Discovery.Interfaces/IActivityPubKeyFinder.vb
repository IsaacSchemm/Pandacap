Imports System.Threading
Imports Microsoft.AspNetCore.Http
Imports Pandacap.ActivityPub.HttpSignatures.Discovery.Models

Public Interface IActivityPubKeyFinder
    ''' <summary>
    ''' Extracts the key ID from the Signature header and fetches the corresponding key.
    ''' </summary>
    ''' <param name="request">The HTTP request</param>
    ''' <param name="cancellationToken">A cancellation token</param>
    ''' <returns>Any found and matching public keys to validaate against</returns>
    Function AcquireKeysAsync(request As HttpRequest,
                              Optional cancellationToken As CancellationToken = Nothing) As IAsyncEnumerable(Of ActorKey)
End Interface
