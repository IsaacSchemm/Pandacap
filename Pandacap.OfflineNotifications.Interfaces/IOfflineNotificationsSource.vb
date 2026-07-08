Imports System.Threading

Public Interface IOfflineNotificationsSource
    Function SyncNotificationsAsync(cancellationToken As CancellationToken) As Task
End Interface
