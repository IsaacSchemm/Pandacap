namespace Pandacap.ATProto.Models

type ATProtoRefUri = {
    Raw: string
} with
    override this.ToString() =
        this.Raw

    member this.Components =
        let array =
            if not (isNull this.Raw) && this.Raw.StartsWith("at://")
            then this.Raw.Split('/')
            else Array.empty

        {|
            DID = Option.toObj (Array.tryItem 2 array)
            Collection = Option.toObj (Array.tryItem 3 array)
            RecordKey = Option.toObj (Array.tryItem 4 array)
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
