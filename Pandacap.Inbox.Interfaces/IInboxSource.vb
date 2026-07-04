Imports System.Threading

Public Interface IInboxSource
    Function ImportNewPostsAsync(Optional cancellationToken As CancellationToken = Nothing) As Task
End Interface
