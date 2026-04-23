Imports System.Threading

Public Interface IDeviantArtClient
    Function GetByUsersYouWatchAsync() As IAsyncEnumerable(Of IDeviation)

    Function GetFavoritesAsync() As IAsyncEnumerable(Of IDeviation)

    Function GetFriendsAsync() As IAsyncEnumerable(Of IRelationship)

    Function GetGalleryFoldersAsync() As IAsyncEnumerable(Of IFolder)

    Function GetHomeFeedAsync() As IAsyncEnumerable(Of IDeviation)

    Function GetMessagesInInboxAsync() As IAsyncEnumerable(Of IMessage)

    Function GetNotesInInboxAsync() As IAsyncEnumerable(Of INote)

    Function GetProfilePostsAsync(username As String) As IAsyncEnumerable(Of IDeviation)

    Function PostArtworkAsync(file As IArtworkFile,
                              title As String,
                              artistComments As String,
                              tags As IEnumerable(Of String),
                              galleryFolders As IEnumerable(Of Guid),
                              isAI As Boolean,
                              disallowThirdPartyAITraining As Boolean,
                              cancellationToken As CancellationToken) As Task(Of IDeviation)

    Function PostJournalAsync(title As String,
                              body As String,
                              tags As IEnumerable(Of String),
                              cancellationToken As CancellationToken) As Task(Of IDeviation)

    Function PostStatusAsync(message As String,
                             cancellationToken As CancellationToken) As Task(Of IDeviation)
End Interface
