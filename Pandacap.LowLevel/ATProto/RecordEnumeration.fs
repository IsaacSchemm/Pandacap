namespace Pandacap.Clients.ATProto

open System
open System.Threading.Tasks

module RecordEnumeration =
    let private findNewestItemsAsync httpClient pds did collection pageSize sample = task {
        let func = XRPC.Com.Atproto.Repo.ListRecordsAsync httpClient pds did collection

        let! forward = func pageSize None ATProtoListDirection.Forward sample
        let! reverse = func pageSize None ATProtoListDirection.Reverse sample

        let page =
            [forward; reverse]
            |> Seq.maxBy (fun page ->
                page.records
                |> Seq.map (fun r -> r.uri)
                |> Seq.tryHead)

        return page.records
    }

    let private thenMapToRecordAsync<'T, 'U> (translate: 'T -> 'U) (t: Task<XRPC.Com.Atproto.Repo.Record<'T>>) = task {
        let! x = t
        return {
            Ref = {
                CID = x.cid
                Uri = { Raw = x.uri }
            }
            Value = translate x.value
        }
    }

    let private thenMapToPageAsync<'T, 'U> (translate: 'T -> 'U) (t: Task<XRPC.Com.Atproto.Repo.Page<'T>>) = task {
        let! l = t
        return {
            Cursor = Option.toObj l.cursor
            Items = [
                for x in l.records do {
                    Ref = {
                        CID = x.cid
                        Uri = { Raw = x.uri }
                    }
                    Value = translate x.value
                }
            ]
        }
    }

    let private thenMapAsync<'T, 'U> (translate: 'T -> 'U) (t: Task<XRPC.Com.Atproto.Repo.Record<'T> list>) = task {
        let! l = t
        return [
            for x in l do {
                Ref = {
                    CID = x.cid
                    Uri = { Raw = x.uri }
                }
                Value = translate x.value
            }
        ]
    }

    module BlueskyProfile =
        let private sample = {|
            avatar = Some {|
                ref = Some {|
                    ``$link`` = ""
                |}
                mimeType = ""
                size = Some 0
                cid = Some ""
            |}
            displayName = Some ""
            description = Some ""
        |}

        let private translate item =
            let _ = [item; sample]

            {
                AvatarCID =
                    item.avatar
                    |> Option.bind (fun a ->
                        a.ref
                        |> Option.map (fun r -> r.``$link``)
                        |> Option.orElse a.cid)
                    |> Option.toObj
                DisplayName = Option.toObj item.displayName
                Description = Option.toObj item.description
            }

        let GetRecordAsync httpClient pds did rkey =
            XRPC.Com.Atproto.Repo.GetRecordAsync httpClient pds did NSIDs.App.Bsky.Actor.Profile rkey sample
            |> thenMapToRecordAsync translate

        let ListRecordsAsync httpClient pds did limit cursor direction =
            XRPC.Com.Atproto.Repo.ListRecordsAsync httpClient pds did NSIDs.App.Bsky.Actor.Profile limit cursor direction sample
            |> thenMapToPageAsync translate

    module BlueskyPost =
        let private sample = {|
            text = ""
            embed = Some {|
                images = Some [{|
                    alt = Some ""
                    image = {|
                        ref = Some {|
                            ``$link`` = ""
                        |}
                        mimeType = ""
                        size = Some 0
                        cid = Some ""
                    |}
                |}]
                record = Some {|
                    cid = ""
                    uri = ""
                |}
            |}
            reply = Some {|
                parent = {|
                    cid = ""
                    uri = ""
                |}
                root = {|
                    cid = ""
                    uri = ""
                |}
            |}
            bridgyOriginalUrl = Some ""
            labels = Some {|
                values = [{|
                    ``val`` = ""
                |}]
            |}
            createdAt = DateTimeOffset.MinValue
            fediverseId = Some ""
        |}

        let private translate item =
            let _ = [item; sample]

            {
                Text = item.text
                Images =
                    item.embed
                    |> Option.bind (fun e -> e.images)
                    |> Option.defaultValue []
                    |> List.map (fun image -> {
                        CID =
                            image.image.ref
                            |> Option.map (fun r -> r.``$link``)
                            |> Option.orElse image.image.cid
                            |> Option.get
                        Alt = image.alt |> Option.defaultValue ""
                    })
                Quoted =
                    item.embed
                    |> Option.bind (fun e -> e.record)
                    |> Option.map (fun r -> {
                        CID = r.cid
                        Uri = { Raw = r.uri }
                    })
                    |> Option.toList
                InReplyTo =
                    item.reply
                    |> Option.map (fun r -> {
                        Parent = {
                            CID = r.parent.cid
                            Uri = { Raw = r.parent.uri }
                        }
                        Root = {
                            CID = r.root.cid
                            Uri = { Raw = r.root.uri }
                        }
                    })
                    |> Option.toList
                BridgyOriginalUrl = Option.toObj item.bridgyOriginalUrl
                FediverseId = Option.toObj item.fediverseId
                Labels =
                    item.labels
                    |> Option.map (fun l -> l.values)
                    |> Option.defaultValue []
                    |> List.map (fun v -> v.``val``)
                CreatedAt = item.createdAt
            }

        let GetRecordAsync httpClient pds did rkey =
            XRPC.Com.Atproto.Repo.GetRecordAsync httpClient pds did NSIDs.App.Bsky.Feed.Post rkey sample
            |> thenMapToRecordAsync translate

        let ListRecordsAsync httpClient pds did limit cursor direction =
            XRPC.Com.Atproto.Repo.ListRecordsAsync httpClient pds did NSIDs.App.Bsky.Feed.Post limit cursor direction sample
            |> thenMapToPageAsync translate

        let FindNewestRecordsAsync httpClient pds did pageSize =
            findNewestItemsAsync httpClient pds did NSIDs.App.Bsky.Feed.Post pageSize sample
            |> thenMapAsync translate

    module BlueskyLike =
        let private sample = {|
            createdAt = DateTimeOffset.MinValue
            subject = {|
                uri = ""
                cid = ""
            |}
        |}

        let private translate item =
            let _ = [item; sample]

            {
                CreatedAt = item.createdAt
                Subject = {
                    CID = item.subject.cid
                    Uri = { Raw = item.subject.uri }
                }
            }

        let GetRecordAsync httpClient pds did rkey =
            XRPC.Com.Atproto.Repo.GetRecordAsync httpClient pds did NSIDs.App.Bsky.Feed.Like rkey sample
            |> thenMapToRecordAsync translate

        let ListRecordsAsync httpClient pds did limit cursor direction =
            XRPC.Com.Atproto.Repo.ListRecordsAsync httpClient pds did NSIDs.App.Bsky.Feed.Like limit cursor direction sample
            |> thenMapToPageAsync translate

        let FindNewestRecordsAsync httpClient pds did pageSize =
            findNewestItemsAsync httpClient pds did NSIDs.App.Bsky.Feed.Like pageSize sample
            |> thenMapAsync translate

    module BlueskyRepost =
        let private sample = {|
            createdAt = DateTimeOffset.MinValue
            subject = {|
                uri = ""
                cid = ""
            |}
        |}

        let private translate item =
            let _ = [item; sample]

            {
                CreatedAt = item.createdAt
                Subject = {
                    CID = item.subject.cid
                    Uri = { Raw = item.subject.uri }
                }
            }

        let GetRecordAsync httpClient pds did rkey =
            XRPC.Com.Atproto.Repo.GetRecordAsync httpClient pds did NSIDs.App.Bsky.Feed.Repost rkey sample
            |> thenMapToRecordAsync translate

        let ListRecordsAsync httpClient pds did limit cursor direction =
            XRPC.Com.Atproto.Repo.ListRecordsAsync httpClient pds did NSIDs.App.Bsky.Feed.Repost limit cursor direction sample
            |> thenMapToPageAsync translate

        let FindNewestRecordsAsync httpClient pds did pageSize =
            findNewestItemsAsync httpClient pds did NSIDs.App.Bsky.Feed.Repost pageSize sample
            |> thenMapAsync translate

    module WhitewindBlogEntry =
        let sample = {|
            content = ""
            createdAt = Some DateTimeOffset.MinValue
            title = Some ""
            visibility = Some ""
        |}

        let translate item =
            let _ = [item; sample]

            {
                Title = Option.toObj item.title
                Content = item.content
                CreatedAt = Option.toNullable item.createdAt
                Public = item.visibility = Some "public"
            }

        let GetRecordAsync httpClient pds did rkey =
            XRPC.Com.Atproto.Repo.GetRecordAsync httpClient pds did NSIDs.Com.Whtwnd.Blog.Entry rkey sample
            |> thenMapToRecordAsync translate

        let ListRecordsAsync httpClient pds did limit cursor direction =
            XRPC.Com.Atproto.Repo.ListRecordsAsync httpClient pds did NSIDs.Com.Whtwnd.Blog.Entry limit cursor direction sample
            |> thenMapToPageAsync translate

        let FindNewestRecordsAsync httpClient pds did pageSize =
            findNewestItemsAsync httpClient pds did NSIDs.Com.Whtwnd.Blog.Entry pageSize sample
            |> thenMapAsync translate

    module LeafletDocument =
        let sample = {|
            pages = [{|
                blocks = [{|
                    ``$type`` = ""
                    block = {|
                        image = Some {|
                            ref = {|
                                ``$link`` = ""
                            |}
                            mimeType = ""
                            size = 0
                        |}
                        aspectRatio = Some {|
                            width = 0
                            height = 0
                        |}

                        plaintext = Some ""

                        src = Some ""
                        title = Some ""
                        description = Some ""
                    |}
                |}]
            |}]
            title = ""
            author = ""
            publication = ""
            publishedAt = DateTimeOffset.MinValue
        |}

        let translate item =
            let _ = [item; sample]

            {
                Pages = [
                    for page in item.pages do {
                        Blocks = [
                            for block in page.blocks do
                                match block.``$type`` with
                                | "pub.leaflet.blocks.image" ->
                                    let image = Option.get block.block.image
                                    LeafletBlock.Image {
                                        CID = image.ref.``$link``
                                        MimeType = image.mimeType
                                        Size = image.size
                                    }

                                | "pub.leaflet.blocks.text" ->
                                    LeafletBlock.Text {
                                        PlainText = Option.get block.block.plaintext
                                    }

                                | "pub.leaflet.blocks.website" ->
                                    LeafletBlock.Website {
                                        Src = Option.get block.block.src
                                        Title = Option.get block.block.title
                                        Description = Option.get block.block.description
                                    }

                                | _ ->
                                    LeafletBlock.Unknown
                        ]
                    }
                ]
                Title = item.title
                Author = item.author
                Publication = { Raw = item.publication }
                PublishedAt = item.publishedAt
            }

        let GetRecordAsync httpClient pds did rkey =
            XRPC.Com.Atproto.Repo.GetRecordAsync httpClient pds did NSIDs.Pub.Leaflet.Document rkey sample
            |> thenMapToRecordAsync translate

        let ListRecordsAsync httpClient pds did limit cursor direction =
            XRPC.Com.Atproto.Repo.ListRecordsAsync httpClient pds did NSIDs.Pub.Leaflet.Document limit cursor direction sample
            |> thenMapToPageAsync translate

        let FindNewestRecordsAsync httpClient pds did pageSize =
            findNewestItemsAsync httpClient pds did NSIDs.Pub.Leaflet.Document pageSize sample
            |> thenMapAsync translate

    module LeafletPublication =
        let sample = {|
            name = ""
            base_path = ""
        |}

        let translate item =
            let _ = [item; sample]

            {
                Name = item.name
                BasePath = item.base_path
            }

        let GetRecordAsync httpClient pds did rkey =
            XRPC.Com.Atproto.Repo.GetRecordAsync httpClient pds did NSIDs.Pub.Leaflet.Publication rkey sample
            |> thenMapToRecordAsync translate
