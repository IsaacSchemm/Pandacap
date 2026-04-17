Imports System.Threading

Public Interface IInboxSource
    Function ImportNewPostsAsync(cancellationToken As CancellationToken) As Task
End Interface
