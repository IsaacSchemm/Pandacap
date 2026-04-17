Imports Pandacap.ActivityPub.Models.Interfaces

Public Interface IActivityPubInteractionTranslator
    Function BuildLikedCollection(postCount As Integer) As String

    Function BuildLikedCollectionPage(currentPageId As String,
                                      posts As IEnumerable(Of IActivityPubLike),
                                      nextPageId As String) As String

    Function BuildLike(likeGuid As Guid,
                       remoteObjectId As String) As String

    Function BuildLikeUndo(likeGuid As Guid,
                           remoteObjectId As String) As String
End Interface
