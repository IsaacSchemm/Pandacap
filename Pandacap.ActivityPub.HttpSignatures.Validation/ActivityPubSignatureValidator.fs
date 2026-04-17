namespace Pandacap.ActivityPub.HttpSignatures.Validation

open System
open System.Security.Cryptography
open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open Pandacap.ActivityPub.HttpSignatures.Discovery.Models
open Pandacap.ActivityPub.HttpSignatures.Validation.Interfaces
open Pandacap.ActivityPub.HttpSignatures.Validation.Models

module internal ActivityPubSignatureValidator =
    module Strings =
        let trimAndRemoveEmpty = StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries

        let splitBy (separators: char array) (str: string) =
            str.Split(separators, trimAndRemoveEmpty)

        let extractCommaSeparatedKeyValuePairs (str: string) = Map.ofList [
            for pair in str.Split(',', trimAndRemoveEmpty) do
                match pair.Split('=', 2, trimAndRemoveEmpty) with
                | [| name; value |] -> yield (name, value)
                | _ -> ()
        ]

    type SignatureElement = {
        KeyId: string
        Signature: string
        SignatureComponents: string list
    }

    let parseHeaderValues (request: HttpRequest) = seq {
        for headerValue in request.Headers["signature"] do
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
                    SignatureComponents =
                        concatenatedHeaderNameString
                        |> Strings.splitBy [| ' '; '"' |]
                        |> List.ofArray
                }
            | _ -> ()
    }

    module SigningDocument =
        let build (components: string seq) (request: HttpRequest) = String.concat "\n" (seq {
            let uri = new Uri(request.GetEncodedUrl())
            for comp in components do
                match comp with
                | "(request-target)" ->
                    yield $"""(request-target): {request.Method.ToLowerInvariant()} {uri.PathAndQuery}"""
                | "host" ->
                    yield $"""host: {uri.Authority.ToLowerInvariant()}"""
                | name ->
                    match request.Headers[name].ToString() with
                    | "" | null -> ()
                    | value ->
                        if name = "content-digest"
                        then yield $"digest: {value}"
                        else yield $"{name}: {value}"
        })

    let verifySignature (request: HttpRequest) (key: IKey) (signatureElement: SignatureElement) =
        use algorithm = RSA.Create()
        algorithm.ImportFromPem(key.KeyPem)

        let signingDocument =
            request
            |> SigningDocument.build signatureElement.SignatureComponents

        algorithm.VerifyData(
            Encoding.UTF8.GetBytes(signingDocument),
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
        let signatureElements = parseHeaderValues request

        let matching =
            signatureElements
            |> Seq.where (fun elem -> Uris.areMatching elem.KeyId key.KeyId)

        if matching |> Seq.isEmpty then
            NoMatchingVerifierFound
        else if matching |> Seq.exists (verifySignature request key) then
            SuccessfullyVerified
        else
            SignatureMismatch

type ActivityPubSignatureValidator() =
    interface IActivityPubSignatureValidator with
        member _.VerifyRequestSignature(request, key) =
            ActivityPubSignatureValidator.verifyRequestSignature request key
