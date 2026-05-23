namespace Pandacap.ATProto.Services

open System
open Pandacap.ATProto.Models

module internal BlueskyRecords =
    open Utility

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

        let asyncFindNewestRecords handler pds did =
            findNewestItems handler pds did "app.bsky.actor.profile" sample
            |> AsyncSeq.toRecordAbstractions translate

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

        let asyncGetRecord handler pds did rkey =
            XRPC.Com.Atproto.Repo.asyncGetRecord handler pds did "app.bsky.feed.post" rkey sample
            |> Async.toRecordAbstraction translate

        let asyncFindNewestRecords handler pds did =
            findNewestItems handler pds did "app.bsky.feed.post" sample
            |> AsyncSeq.toRecordAbstractions translate

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

        let asyncFindNewestRecords handler pds did =
            findNewestItems handler pds did "app.bsky.feed.like" sample
            |> AsyncSeq.toRecordAbstractions translate

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

        let asyncFindNewestRecords handler pds did =
            findNewestItems handler pds did "app.bsky.feed.repost" sample
            |> AsyncSeq.toRecordAbstractions translate
