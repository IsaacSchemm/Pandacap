Imports System.Threading
Imports Microsoft.AspNetCore.Http
Imports Pandacap.ActivityPub.HttpSignatures.Discovery.Models
Imports Pandacap.ActivityPub.HttpSignatures.Validation.Models

Public Interface IActivityPubSignatureValidator
    Function VerifyRequestSignatureAsync(message As HttpRequest,
                                         key As IKey,
                                         Optional cancellationToken As CancellationToken = Nothing) As Task(Of VerificationResult)
End Interface
