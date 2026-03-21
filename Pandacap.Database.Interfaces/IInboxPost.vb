Public Interface IInboxPost
    Inherits IPost

    ReadOnly Property DismissedAt As DateTimeOffset?
    ReadOnly Property IsPodcast As Boolean
    ReadOnly Property IsShare As Boolean
End Interface
