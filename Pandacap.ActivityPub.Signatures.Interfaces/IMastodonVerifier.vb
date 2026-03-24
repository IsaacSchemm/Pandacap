Imports Microsoft.AspNetCore.Http
Imports NSign

Public Interface IMastodonVerifier
    Function VerifyRequestSignature(message As HttpRequest,
                                    key As IKey) As VerificationResult
End Interface
