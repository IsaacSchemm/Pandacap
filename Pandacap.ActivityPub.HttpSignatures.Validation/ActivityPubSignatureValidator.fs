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
    module Strings =
        let trimAndRemoveEmpty = StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries

        let splitBy (separators: char seq) (str: string) =
            str.Split(Array.ofSeq separators, trimAndRemoveEmpty)

        let rec deparenthesize (str: string) =
            if str.StartsWith('(') && str.EndsWith(')')
            then str.Substring(1, str.Length - 2)
            else str

        let extractCommaSeparatedKeyValuePairs (str: string) = Map.ofList [
            for pair in str.Split(',', trimAndRemoveEmpty) do
                match pair.Split('=', 2, trimAndRemoveEmpty) with
                | [| name; value |] -> yield (name, value)
                | _ -> ()
        ]

    type SignatureElement = {
        Spec: SignatureInputSpec
        KeyId: string
        Signature: string
    }

    module Spec =
        let toComponent (name: string): SignatureComponent =
            match name with
            | "authority"
            | "status"
            | "request-target"
            | "target-uri"
            | "path"
            | "method"
            | "query"
            | "scheme"
            | "query-param"
            | "signature-params" ->
                new DerivedComponent($"@{name}")
            | _ ->
                new HttpHeaderComponent(name)

        let parse (names: string) =
            let spec = new SignatureInputSpec("spec")
            for name in names |> Strings.splitBy [' '; '"'] do
                name
                |> Strings.deparenthesize
                |> toComponent
                |> spec.SignatureParameters.AddComponent
                |> ignore
            spec

    let parseHeaderValues (request: HttpRequest) = seq {
        for headerValue in request.Headers[Constants.Headers.Signature] do
            let pairs = Strings.extractCommaSeparatedKeyValuePairs headerValue

            match
                pairs |> Map.tryFind "keyId",
                pairs |> Map.tryFind "signature",
                pairs |> Map.tryFind "headers"
            with
            | Some keyId, Some signature, Some concatenatedHeaderNameString ->
                yield {
                    KeyId = keyId.Trim('"')
                    Signature = signature.Trim('"')
                    Spec = Spec.parse concatenatedHeaderNameString
                }
            | _ -> ()
    }

    let verifySignature (componentBuilder: ComponentBuilder) (key: IKey) (signatureElement: SignatureElement) =
        use algorithm = RSA.Create()
        algorithm.ImportFromPem(key.KeyPem)

        let visitor: ISignatureComponentVisitor = componentBuilder
        visitor.Visit(signatureElement.Spec.SignatureParameters)

        algorithm.VerifyData(
            Encoding.UTF8.GetBytes(componentBuilder.SigningDocument),
            Convert.FromBase64String(signatureElement.Signature),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1)

    module Uris =
        let (|ValidUri|_|) (str: string) =
            match Uri.TryCreate(str, UriKind.Absolute) with
            | true, uri -> Some uri
            | false, _ -> None

        let rec areMatching str1 str2 =
            match (str1, str2) with
            | (ValidUri str1, ValidUri str2) when str1 = str2 -> true
            | _ -> false

    let verifyRequestSignature request (key: IKey) =
        let componentBuilder = new ComponentBuilder(request)
        let signatureElements = parseHeaderValues request

        let matching =
            signatureElements
            |> Seq.where (fun elem -> Uris.areMatching elem.KeyId key.KeyId)

        if matching |> Seq.isEmpty then
            VerificationResult.NoMatchingVerifierFound
        else if matching |> Seq.exists (verifySignature componentBuilder key) then
            VerificationResult.SuccessfullyVerified
        else
            VerificationResult.SignatureMismatch

type ActivityPubSignatureValidator() =
    interface IActivityPubSignatureValidator with
        member _.VerifyRequestSignature(request, key) =
            ActivityPubSignatureValidator.verifyRequestSignature request key
