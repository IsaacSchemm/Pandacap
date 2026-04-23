Imports DeviantArtFs

Public Interface IDeviantArtCredentialProvider
    Function GetTokensAsync() As IAsyncEnumerable(Of IDeviantArtRefreshableAccessToken)
End Interface
