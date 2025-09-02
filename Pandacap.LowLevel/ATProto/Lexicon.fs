namespace Pandacap.Clients.ATProto

open System
open System.Text.Json.Serialization

module Lexicon =
    let private getComponents (uri: string) =
        let split =
            uri
            |> Option.ofObj
            |> Option.filter (fun uri -> uri.StartsWith("at://"))
            |> Option.map (fun str -> str.Split('/'))
            |> Option.defaultValue [||]

        {|
            DID = split |> Seq.tryItem 2 |> Option.toObj
            Collection = split |> Seq.tryItem 3 |> Option.toObj
            RecordKey = split |> Seq.tryItem 4 |> Option.toObj
        |}

    type Ref = {
        ``$link``: string
    }

    type Blob = {
        ref: Ref option
        mimeType: string
        size: int option

        // https://atproto.com/specs/data-model#blob-type
        cid: string option
    } with
        [<JsonIgnore>]
        member this.CID =
            this.ref
            |> Option.map (fun r -> r.``$link``)
            |> Option.orElse this.cid
            |> Option.toObj

    type ITokens =
        abstract member AccessToken: string
        abstract member RefreshToken: string
        abstract member Handle: string

    type IRecord<'T> =
        abstract member CID: string
        abstract member DID: string
        abstract member RecordKey: string
        abstract member Value: 'T

    module Com =
        module Atproto =
            module Repo =
                type StrongRef = {
                    cid: string
                    uri: string
                } with
                    [<JsonIgnore>]
                    member this.DID = (getComponents this.uri).DID

                    [<JsonIgnore>]
                    member this.RecordKey = (getComponents this.uri).RecordKey

                type GetRecord<'T> = {
                    uri: string
                    cid: string
                    value: 'T
                } with
                    [<JsonIgnore>]
                    member this.DID = (getComponents this.uri).DID

                    [<JsonIgnore>]
                    member this.RecordKey = (getComponents this.uri).RecordKey

                    interface IRecord<'T> with
                        member this.CID = this.cid
                        member this.DID = this.DID
                        member this.RecordKey = this.RecordKey
                        member this.Value = this.value

                type ListRecords<'T> = {
                    records: GetRecord<'T> list
                    cursor: string option
                } with
                    [<JsonIgnore>]
                    member this.Cursor = Option.toObj this.cursor

            module Server =
                type RefreshSession = {
                    accessJwt: string
                    refreshJwt: string
                    handle: string
                    did: string
                } with
                    interface ITokens with
                        member this.AccessToken = this.accessJwt
                        member this.RefreshToken = this.refreshJwt
                        member this.Handle = this.handle

    type IHasSubject =
        abstract member Subject: Com.Atproto.Repo.StrongRef

    module App =
        module Bsky =
            module Actor =
                type Profile = {
                    displayName: string option
                    description: string option
                    avatar: Blob option
                } with
                    [<JsonIgnore>]
                    member this.DisplayName = Option.toObj this.displayName

                    [<JsonIgnore>]
                    member this.Avatar = Option.toObj this.avatar

                    [<JsonIgnore>]
                    member this.Description = Option.toObj this.description

                module Defs =
                    type ProfileView = {
                        did: string
                        handle: string
                        displayName: string option
                        avatar: string option
                        description: string option
                    } with
                        [<JsonIgnore>]
                        member this.DisplayName = Option.toObj this.displayName

                        [<JsonIgnore>]
                        member this.Avatar = Option.toObj this.avatar

                        [<JsonIgnore>]
                        member this.Description = Option.toObj this.description

            module Feed =
                module Post =
                    type Image = {
                        alt: string option
                        image: Blob
                    } with
                        [<JsonIgnore>]
                        member this.Alt = this.alt |> Option.toObj

                    type Embed = {
                        images: Image list option
                        record: Com.Atproto.Repo.StrongRef option
                    }

                    type Reply = {
                        parent: Com.Atproto.Repo.StrongRef
                        root: Com.Atproto.Repo.StrongRef
                    }

                    type Label = {
                        ``val``: string
                    }

                    type Labels = {
                        values: Label list
                    }

                type Post = {
                    text: string
                    embed: Post.Embed option
                    reply: Post.Reply option
                    bridgyOriginalUrl: string option
                    labels: Post.Labels option
                    createdAt: DateTimeOffset
                } with
                    [<JsonIgnore>]
                    member this.Images =
                        this.embed
                        |> Option.bind (fun e -> e.images)
                        |> Option.defaultValue []

                    [<JsonIgnore>]
                    member this.EmbeddedRecord =
                        this.embed
                        |> Option.bind (fun e -> e.record)
                        |> Option.toObj

                    [<JsonIgnore>]
                    member this.InReplyTo =
                        Option.toObj this.reply

                    [<JsonIgnore>]
                    member this.Labels =
                        this.labels
                        |> Option.map (fun ls -> ls.values)
                        |> Option.defaultValue []
                        |> Seq.map (fun l -> l.``val``)

                    [<JsonIgnore>]
                    member this.BridgyOriginalUrl =
                        Option.toObj this.bridgyOriginalUrl

                type Like = {
                    createdAt: DateTimeOffset
                    subject: Com.Atproto.Repo.StrongRef
                } with
                    interface IHasSubject with
                        member this.Subject = this.subject

                type Repost = {
                    createdAt: DateTimeOffset
                    subject: Com.Atproto.Repo.StrongRef
                } with
                    interface IHasSubject with
                        member this.Subject = this.subject

                module Actor =
                    type Profile = {
                        avatar: Blob option
                        banner: Blob option
                        displayName: string option
                        description: string option
                    } with
                        [<JsonIgnore>]
                        member this.Avatar = Option.toObj this.avatar

                        [<JsonIgnore>]
                        member this.Banner = Option.toObj this.banner

                        [<JsonIgnore>]
                        member this.DisplayName = Option.toObj this.displayName

                        [<JsonIgnore>]
                        member this.Description = Option.toObj this.description

            module Notification =
                module ListNotifications =
                    type Notification = {
                        uri: string
                        cid: string
                        author: Actor.Defs.ProfileView
                        reason: string
                        reasonSubject: string
                        isRead: bool
                        indexedAt: DateTimeOffset
                    } with
                        [<JsonIgnore>]
                        member this.DID = (getComponents this.uri).DID

                        [<JsonIgnore>]
                        member this.RecordKey = (getComponents this.uri).RecordKey

                        [<JsonIgnore>]
                        member this.ReasonSubject = {|
                            DID = (getComponents this.reasonSubject).DID
                            Collection = (getComponents this.reasonSubject).Collection
                            RecordKey = (getComponents this.reasonSubject).RecordKey
                        |}

                type ListNotifications = {
                    cursor: string option
                    notifications: ListNotifications.Notification list
                } with
                    [<JsonIgnore>]
                    member this.Cursor =
                        this.cursor
                        |> Option.toObj
