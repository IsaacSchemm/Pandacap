Imports System.Threading

Public Interface IOutboxDestination
    Function PublishNextQueuedPostAsync(cancellationToken As CancellationToken) As Task(Of Boolean)
    Function SynchronizeFoldersAsync(cancellationToken As CancellationToken) As Task
End Interface
