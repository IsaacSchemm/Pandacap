Imports Pandacap.ActivityPub.Models.Interfaces

Public Interface IActivityPubRelationshipTranslator
    Function BuildFollowAccept(followId As String) As String

    Function BuildFollowersCollection(followersCount As Integer) As String

    Function BuildFollowingCollection(following As IEnumerable(Of IActivityPubFollow)) As String

    Function BuildFollow(followGuid As Guid, remoteActorId As String) As String

    Function BuildFollowUndo(followGuid As Guid, remoteActorId As String) As String
End Interface
