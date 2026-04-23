Public Interface IDeviation
    ReadOnly Property Author As IAuthor
    ReadOnly Property DeviationId As Guid
    ReadOnly Property Excerpt As String
    ReadOnly Property IsMature As Boolean
    ReadOnly Property PublishedTime As DateTimeOffset?
    ReadOnly Property Thumbnails As IEnumerable(Of String)
    ReadOnly Property Title As String
    ReadOnly Property Url As String
End Interface
