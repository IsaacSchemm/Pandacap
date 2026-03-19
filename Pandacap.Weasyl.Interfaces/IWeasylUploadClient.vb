Imports System.Threading
Imports Pandacap.Weasyl.Models
Imports Pandacap.Weasyl.Models.WeasylUpload

Public Interface IWeasylUploadClient
    Function GetFoldersAsync(cancellationToken As CancellationToken) As IAsyncEnumerable(Of Folder)

    Function UploadVisualAsync(url As String,
                               title As String,
                               subtype As SubmissionType,
                               folderid As Integer?,
                               rating As Rating,
                               content As String,
                               tags As IEnumerable(Of String),
                               cancellationToken As CancellationToken) As Task(Of Integer?)

    Function UploadJournalAsync(title As String,
                                rating As Rating,
                                content As String,
                               tags As IEnumerable(Of String),
                               cancellationToken As CancellationToken) As Task(Of Integer?)
End Interface
