Imports System.Threading
Imports Newtonsoft.Json.Linq

Public Interface IActivityPubInboxRequestHandler
    Function ProcessVerifiedInboxMessageAsync(expandedObject As JToken,
                                              myActorId As String,
                                              cancellationToken As CancellationToken) As Task
End Interface
