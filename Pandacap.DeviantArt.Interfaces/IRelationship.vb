Public Interface IRelationship
    ReadOnly Property AreYouWatching As Boolean
    ReadOnly Property IsWatchingYou As Boolean
    ReadOnly Property LastVisit As DateTimeOffset?
    ReadOnly Property Username As String
End Interface
