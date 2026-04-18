Imports Pandacap.UI.Badges

Public Interface INotification
    ReadOnly Property ActivityName As String
    ReadOnly Property Badge As Badge
    ReadOnly Property Url As String
    ReadOnly Property UserName As String
    ReadOnly Property UserUrl As String
    ReadOnly Property PostUrl As String
    ReadOnly Property Timestamp As DateTimeOffset
End Interface
