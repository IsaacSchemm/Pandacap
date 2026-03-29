namespace Pandacap.ActivityPub.HttpSignatures.Validation.Models

type VerificationResult =
| SuccessfullyVerified
| SignatureMismatch
| NoMatchingVerifierFound
