Imports System.Threading
Imports Pandacap.ActivityPub.RemoteObjects.Models

Public Interface IActivityPubRemoteActorService
    Function FetchActorAsync(url As String,
                             cancellationToken As CancellationToken) As Task(Of RemoteActor)

    Function FetchAddresseeAsync(url As String,
                                 cancellationToken As CancellationToken) As Task(Of RemoteAddressee)

    Function FetchAddresseesAsync(urls As IEnumerable(Of String),
                                  cancellationToken As CancellationToken) As Task(Of RemoteAddressee())
End Interface
