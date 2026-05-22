Imports Pandacap.ActivityPub.Models.Interfaces
Imports Pandacap.ActivityPub.RemoteObjects.Models

Public Interface IActivityPubInboxAddressingFilter
    Function IsIncludedInInbox(post As RemotePost,
                               relationship As IActivityPubFollow) As Boolean
End Interface
