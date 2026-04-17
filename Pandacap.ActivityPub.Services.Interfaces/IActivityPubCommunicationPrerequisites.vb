Imports System.Threading

Public Interface IActivityPubCommunicationPrerequisites
    ReadOnly Property UserAgent As String

    Function GetPublicKeyAsync(cancellationToken As CancellationToken) As Task(Of String)

    Function SignRsaSha256Async(data As Byte(),
                                cancellationToken As CancellationToken) As Task(Of Byte())
End Interface
