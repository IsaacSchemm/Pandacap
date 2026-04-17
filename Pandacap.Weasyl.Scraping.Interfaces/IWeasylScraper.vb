Imports Microsoft.FSharp.Collections
Imports Pandacap.Weasyl.Scraping.Models

Public Interface IWeasylScraper
    Function ExtractFavoriteSubmitids(html As String) As SubmissionsPage

    Function ExtractNotificationGroups(html As String) As FSharpList(Of NotificationGroupCollection)

    Function ExtractNotifications(html As String) As FSharpList(Of ExtractedNotification)

    Function ExtractJournals(html As String) As FSharpList(Of ExtractedJournal)

    Function ExtractNotes(html As String) As FSharpList(Of ExtractedNote)
End Interface
