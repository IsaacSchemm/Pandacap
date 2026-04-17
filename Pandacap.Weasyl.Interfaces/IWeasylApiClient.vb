Imports System.Threading
Imports Pandacap.Weasyl.Models.WeasylApi

Public Interface IWeasylApiClient
    Function WhoamiAsync(cancellationToken As CancellationToken) As Task(Of WhoamiResponse)

    Function GetAvatarAsync(username As String,
                            cancellationToken As CancellationToken) As Task(Of AvatarResponse)

    Function ViewSubmissionAsync(submitid As Integer,
                                 cancellationToken As CancellationToken) As Task(Of Submission)

    Function GetMessagesSubmissionsAsync(cancellationToken As CancellationToken) As IAsyncEnumerable(Of Submission)

    Function GetMessagesSummaryAsync(cancellationToken As CancellationToken) As Task(Of MessagesSummary)
End Interface
