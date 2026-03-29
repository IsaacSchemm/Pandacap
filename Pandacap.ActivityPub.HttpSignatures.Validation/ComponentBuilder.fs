namespace Pandacap.ActivityPub.HttpSignatures.Validation

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open NSign.Signatures

type DC = NSign.Constants.DerivedComponents

type ComponentBuilder(req: HttpRequest) =
    let values = new ResizeArray<string>()

    let getDerivedComponentValue (derivedComponent: DerivedComponent) =
        let uri = lazy (new Uri(req.GetEncodedUrl()))

        match derivedComponent.ComponentName with
        | DC.Method -> req.Method.ToLowerInvariant()
        | DC.TargetUri -> uri.Value.OriginalString
        | DC.Authority -> uri.Value.Authority.ToLowerInvariant()
        | DC.Scheme -> uri.Value.Scheme.ToLowerInvariant()
        | DC.RequestTarget -> uri.Value.PathAndQuery
        | DC.Path -> uri.Value.AbsolutePath
        | DC.Query ->
            match uri.Value.Query with
            | null | "" -> "?"
            | _ -> uri.Value.Query
        | n -> failwithf "Cannot use the value of the unimplmented derived component %s in validating a signature." n

    interface ISignatureComponentVisitor with
        member _.Visit(_: SignatureComponent) = ()

        member _.Visit(httpHeader: HttpHeaderComponent): unit = 
            match httpHeader.ComponentName with
            | "host" ->
                values.Add($"host: {getDerivedComponentValue SignatureComponent.Authority}")
            | name ->
                match req.Headers[name].ToString() with
                | "" | null -> ()
                | value ->
                    let nameToUse =
                        match name with
                        | "content-digest" -> "digest"
                        | _ -> name
                    values.Add($"{nameToUse}: {value}")

        member _.Visit(_: HttpHeaderDictionaryStructuredComponent) = ()

        member _.Visit(_: HttpHeaderStructuredFieldComponent) = ()

        member _.Visit(derived: DerivedComponent) =
            match derived.ComponentName with
            | DC.RequestTarget ->
                values.Add($"(request-target): {getDerivedComponentValue SignatureComponent.Method} {getDerivedComponentValue derived}")
            | DC.Authority ->
                values.Add($"host: {getDerivedComponentValue derived}")
            | _ -> ()

        member this.Visit(sigParams: SignatureParamsComponent) = 
            for comp in sigParams.Components do
                comp.Accept(this)

            let hasRequestTarget =
                sigParams.Components
                |> Seq.map (fun c -> c.ComponentName)
                |> Seq.contains DC.RequestTarget

            if not hasRequestTarget then
                SignatureComponent.RequestTarget.Accept(this)

        member _.Visit(_: QueryParamComponent) = ()

    member _.SigningDocument = String.concat "\n" values
