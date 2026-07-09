Imports System.Threading

Public Interface IActivityPubOutboxProcessor
    Function AttemptToSendPendingActivityAsync(id As Guid,
                                               Optional cancellationToken As CancellationToken = Nothing) As Task
End Interface
