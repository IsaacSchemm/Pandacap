Imports System.Threading
Imports Microsoft.AspNetCore.Http
Imports NSign
Imports Pandacap.ActivityPub.Signatures.Interfaces

Public Interface IActivityAuthenticator
    ''' <summary>
    ''' Extracts the key ID from the Signature header and fetches the corresponding key.
    ''' </summary>
    ''' <param name="request">The HTTP request</param>
    ''' <param name="cancellationToken">A cancellation token</param>
    ''' <returns></returns>
    Function AcquireKeyAsync(request As HttpRequest,
                             cancellationToken As CancellationToken) As Task(Of IKeyWithOwner)
End Interface
