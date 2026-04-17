Imports Pandacap.ActivityPub.Models

Public Interface IActivityPubProfileTranslator
    Function BuildProfile(profile As ActivityPubProfile) As String

    Function BuildProfileUpdate(profile As ActivityPubProfile) As String
End Interface
