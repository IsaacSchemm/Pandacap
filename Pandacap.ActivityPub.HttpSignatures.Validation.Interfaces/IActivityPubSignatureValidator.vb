Imports Microsoft.AspNetCore.Http
Imports NSign

Public Interface IActivityPubSignatureValidator
    Function VerifyRequestSignature(message As HttpRequest,
                                    key As IKey) As VerificationResult
End Interface
