Imports Microsoft.AspNetCore.Http
Imports Pandacap.ActivityPub.HttpSignatures.Discovery.Models
Imports Pandacap.ActivityPub.HttpSignatures.Validation.Models

Public Interface IActivityPubSignatureValidator
    Function VerifyRequestSignature(message As HttpRequest,
                                    key As IKey) As VerificationResult
End Interface
