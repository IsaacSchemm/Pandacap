Imports System.Threading
Imports Microsoft.AspNetCore.Http
Imports NSign
Imports Pandacap.ActivityPub.Models.Inbound
Imports Pandacap.ActivityPub.Signatures.Interfaces

Public Interface IActivityPubMessageAuthenticator
    ''' <summary>
    ''' Extracts the key ID from the Signature header and fetches the corresponding key.
    ''' </summary>
    ''' <param name="request"></param>
    ''' <param name="cancellationToken"></param>
    ''' <returns></returns>
    Function AcquireKeyAsync(request As HttpRequest,
                             cancellationToken As CancellationToken) As Task(Of IKeyWithOwner)

    ''' <summary>
    ''' Checks the HTTP signature against the given key. Does not attempt to verify that the ActivityPub actor matches.
    ''' </summary>
    ''' <param name="key"></param>
    ''' <param name="request"></param>
    ''' <param name="cancellationToken"></param>
    ''' <returns></returns>
    Function AuthenticateAsync(key As IKey,
                               request As HttpRequest,
                               cancellationToken As CancellationToken) As Task(Of VerificationResult)

    ''' <summary>
    ''' Checks whether the key corresponds to the ActivityPub actor.
    ''' </summary>
    ''' <param name="key"></param>
    ''' <param name="actor"></param>
    ''' <param name="cancellationToken"></param>
    ''' <returns></returns>
    Function Authorize(key As IKeyWithOwner,
                       actor As RemoteActor,
                       cancellationToken As CancellationToken) As Boolean
End Interface
