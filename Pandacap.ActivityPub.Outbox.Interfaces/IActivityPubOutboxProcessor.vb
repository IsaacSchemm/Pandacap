Imports System.Threading

Public Interface IActivityPubOutboxProcessor
    Function SendPendingActivitiesAsync(cancellationToken As CancellationToken) As Task
End Interface
