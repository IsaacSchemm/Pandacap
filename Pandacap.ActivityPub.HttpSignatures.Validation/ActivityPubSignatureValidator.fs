namespace Pandacap.ActivityPub.HttpSignatures.Validation

open System
open System.Security.Cryptography
open System.Text
open Microsoft.AspNetCore.Http
open NSign
open NSign.Signatures
open Pandacap.ActivityPub.HttpSignatures
open Pandacap.ActivityPub.HttpSignatures.Validation.Interfaces

module internal ActivityPubSignatureValidator =
    let allDerivedComponents = [
        Constants.DerivedComponents.Authority
        Constants.DerivedComponents.Status
        Constants.DerivedComponents.RequestTarget
        Constants.DerivedComponents.TargetUri
        Constants.DerivedComponents.Path
        Constants.DerivedComponents.Method
        Constants.DerivedComponents.Query
        Constants.DerivedComponents.Scheme
        Constants.DerivedComponents.QueryParam
        Constants.DerivedComponents.SignatureParams
    ]

    type SignatureElement =
    | Spec of SignatureInputSpec
    | KeyId of string
    | Signature of string

    let verifySignature (componentBuilder: ComponentBuilder) (key: IKey) (signatureElements: SignatureElement seq) =
        let spec = List.head [for e in signatureElements do match e with Spec x -> x | _ -> ()]
        let signature = List.head [for e in signatureElements do match e with Signature x -> x | _ -> ()]

        use algorithm = RSA.Create()
        algorithm.ImportFromPem(key.KeyPem)

        (componentBuilder :> ISignatureComponentVisitor).Visit(spec.SignatureParameters)

        algorithm.VerifyData(
            Encoding.UTF8.GetBytes(componentBuilder.SigningDocument),
            Convert.FromBase64String(signature),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1)

    let rec deparenthesize (str: string) =
        if str.StartsWith('(') && str.EndsWith(')')
        then str.Substring(1, str.Length - 2)
        else str

    let parseHeaderValue (headerValue: string) = [
        let splitOptions = StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries

        for keyValuePair in headerValue.Split(',', splitOptions) do
            match keyValuePair.Split('=', 2, splitOptions) with
            | [| "keyId"; value |] ->
                yield KeyId (value.Trim('"'))
            | [| "signature"; value |] ->
                yield Signature (value.Trim('"'))
            | [| "headers"; value |] ->
                let spec = new SignatureInputSpec("spec")
                for str in value.Split([| ' '; '"' |], splitOptions) do
                    let name = deparenthesize str
                    spec.SignatureParameters.AddComponent(
                        if allDerivedComponents |> List.contains name then new DerivedComponent(name) :> SignatureComponent
                        else if allDerivedComponents |> List.contains $"@{name}" then new DerivedComponent($"@{name}")
                        else new HttpHeaderComponent(name)) |> ignore
                yield Spec spec
            | _ -> ()
    ]

    let parseHeaderValues (request: HttpRequest) = seq {
        for headerValue in request.Headers[Constants.Headers.Signature] do
            parseHeaderValue headerValue
    }

type ActivityPubSignatureValidator() =
    let tryCreateUri (str: string) =
        match Uri.TryCreate(str, UriKind.Absolute) with
        | true, uri -> Some uri
        | false, _ -> None

    interface IActivityPubSignatureValidator with
        member this.VerifyRequestSignature(request, key) = Seq.head (seq {
            let builder = new ComponentBuilder(request)
            let parsedHeaders = ActivityPubSignatureValidator.parseHeaderValues request
            let mutable defaultResult = VerificationResult.NoMatchingVerifierFound

            for parsedHeader in parsedHeaders do
                let keyId = List.head [for e in parsedHeader do match e with ActivityPubSignatureValidator.KeyId x -> x | _ -> ()]
                
                match tryCreateUri keyId, tryCreateUri key.KeyId with
                | Some uri1, Some uri2 when uri1 = uri2 ->
                    if ActivityPubSignatureValidator.verifySignature builder key parsedHeader then
                        yield VerificationResult.SuccessfullyVerified
                    else
                        defaultResult <- VerificationResult.SignatureMismatch
                | _ -> ()

            yield defaultResult
        })
