Imports System.Threading
Imports Microsoft.FSharp.Collections
Imports Pandacap.FurAffinity.Models

Public Interface IFurAffinityClient
    Function WhoamiAsync(cancellationToken As CancellationToken) As Task(Of String)

    Function GetTimeZoneAsync(cancellationToken As CancellationToken) As Task(Of TimeZoneInfo)

    Function ListPostOptionsAsync(cancellationToken As CancellationToken) As Task(Of PostOptionsCollection)

    Function ListGalleryFoldersAsync(cancellationToken As CancellationToken) As Task(Of FSharpList(Of GalleryFolder))

    Function PostArtworkAsync(file As Byte(),
                              metadata As ArtworkMetadata,
                              cancellationToken As CancellationToken) As Task(Of Uri)

    Function GetFavoritesAsync(name As String,
                               pagination As FavoritesPage,
                               cancellationToken As CancellationToken) As Task(Of FSharpList(Of Submission))

    Function GetSubmissionsAsync(pagination As SubmissionsPage,
                                 cancellationToken As CancellationToken) As Task(Of FSharpList(Of Submission))

    Function GetNotesAsync(cancellationToken As CancellationToken) As Task(Of FSharpList(Of Note))

    Function GetJournalAsync(journalId As Long,
                             cancellationToken As CancellationToken) As Task(Of Journal)

    Function PostJournalAsync(subject As String,
                              message As String,
                              rating As Rating,
                              cancellationToken As CancellationToken) As Task(Of Uri)

    Function GetNotificationsAsync(cancellationToken As CancellationToken) As Task(Of FSharpList(Of Notification))
End Interface
