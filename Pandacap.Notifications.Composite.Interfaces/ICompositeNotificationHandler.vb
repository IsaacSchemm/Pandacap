Imports Pandacap.Notifications.Interfaces

Public Interface ICompositeNotificationHandler
    Function GetNotificationsAsync() As IAsyncEnumerable(Of INotification)
End Interface
