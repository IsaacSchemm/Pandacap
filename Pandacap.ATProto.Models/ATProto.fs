namespace Pandacap.ATProto.Models

type ATProtoRefUri = {
    Raw: string
} with
    override this.ToString() =
        this.Raw

    member this.Components =
        let split =
            this.Raw
            |> Option.ofObj
            |> Option.filter (fun uri -> uri.StartsWith("at://"))
            |> Option.map (fun str -> str.Split('/'))
            |> Option.defaultValue [||]

        {|
            DID = split |> Seq.tryItem 2 |> Option.toObj
            Collection = split |> Seq.tryItem 3 |> Option.toObj
            RecordKey = split |> Seq.tryItem 4 |> Option.toObj
        |}

type ATProtoRef = {
    CID: string
    Uri: ATProtoRefUri
}

type ATProtoRecord<'T> = {
    Ref: ATProtoRef
    Value: 'T
}

type ATProtoPage<'T> = {
    Items: 'T list
    Cursor: string
}

type ATProtoListDirection =
| Forward
| Reverse
