Imports Pandacap.Database

Public Interface IFeedReader
    Function ReadFeedAsync(uri As String,
                           contentType As String) As IAsyncEnumerable(Of GeneralInboxItem)
End Interface
