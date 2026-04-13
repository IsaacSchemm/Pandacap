Public Interface IInboxSourceFactory
    Function GetInboxSourcesForPlatformAsync() As IAsyncEnumerable(Of IInboxSource)
End Interface
