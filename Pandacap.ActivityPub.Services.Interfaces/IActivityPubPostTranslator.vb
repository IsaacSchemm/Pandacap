Imports Pandacap.ActivityPub.Models.Interfaces

Public Interface IActivityPubPostTranslator
    Function BuildObject(post As IActivityPubPost) As String
    Function BuildObjectCreate(post As IActivityPubPost) As String
    Function BuildObjectUpdate(post As IActivityPubPost) As String
    Function BuildObjectDelete(post As IActivityPubPost) As String

    Function BuildOutboxCollection(postCount As Integer) As String

    Function BuildOutboxCollectionPage(currentPageId As String,
                                       posts As IEnumerable(Of IActivityPubPost),
                                       nextPageId As String) As String
End Interface
