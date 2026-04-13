Imports DeviantArtFs

Public Interface IDeviantArtCredentialProvider
    Function GetTokenAsync() As Task(Of IDeviantArtRefreshableAccessToken)
    Function GetUserAsync() As Task(Of ResponseTypes.User)
End Interface
