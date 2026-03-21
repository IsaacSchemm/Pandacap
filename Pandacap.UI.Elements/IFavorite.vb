Public Interface IFavorite
    Inherits IPost

    ReadOnly Property FavoritedAt As DateTimeOffset
    ReadOnly Property HiddenAt As DateTimeOffset?
End Interface
