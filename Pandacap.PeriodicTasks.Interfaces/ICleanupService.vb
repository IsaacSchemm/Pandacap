Imports System.Threading

Public Interface ICleanupService
    ''' <summary>
    ''' Finds active (non-dismissed) inbox posts, past the first 200, which are at least 30 days old, and dismiss them.
    ''' </summary>
    Function DismissOldPostsAsync(Optional cancellationToken As CancellationToken = Nothing) As Task

    ''' <summary>
    ''' Finds inbox posts that were dismissed more than a day ago, and are more than a week old,
    ''' and deletes all but the most recent of each type.
    ''' </summary>
    Function RemoveDismissedPostsAsync(Optional cancellationToken As CancellationToken = Nothing) As Task

    ''' <summary>
    ''' Deletes unsent outbound ActivityPub activities that are more than a week old.
    ''' </summary>
    Function RemoveOldOutboundActivitiesAsync(Optional cancellationToken As CancellationToken = Nothing) As Task
End Interface
