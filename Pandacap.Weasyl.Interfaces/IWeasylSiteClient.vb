Imports System.Threading
Imports Microsoft.FSharp.Collections
Imports Pandacap.Weasyl.Scraping.Models

Public Interface IWeasylSiteClient
    Function ExtractFavoriteSubmitidsAsync(userid As Integer,
                                           cancellationToken As CancellationToken) As IAsyncEnumerable(Of Integer)

    Function ExtractJournalsAsync(cancellationToken As CancellationToken) As Task(Of FSharpList(Of ExtractedJournal))

    Function ExtractNotificationsAsync(cancellationToken As CancellationToken) As Task(Of FSharpList(Of ExtractedNotification))

    Function GetNotesAsync(cancellationToken As CancellationToken) As Task(Of FSharpList(Of ExtractedNote))
End Interface
