Public Interface INotificationHandler
    Function GetNotificationsAsync() As IAsyncEnumerable(Of INotification)
End Interface
