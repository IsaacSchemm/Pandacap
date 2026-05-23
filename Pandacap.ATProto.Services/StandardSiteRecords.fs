namespace Pandacap.ATProto.Services

open System
open Pandacap.ATProto.Models

module internal StandardSiteRecords =
    open Utility

    module Publication =
        let private sample = {|
            url = ""
            icon = Some {|
                ref = Some {|
                    ``$link`` = ""
                |}
                mimeType = ""
                size = Some 0
                cid = Some ""
            |}
            name = ""
            description = Some ""
        |}

        let private translate item =
            let _ = [item; sample]

            {
                Url = item.url
                Icon =
                    item.icon
                    |> Option.bind (fun icon ->
                        icon.ref
                        |> Option.map (fun r -> r.``$link``)
                        |> Option.orElse icon.cid)
                    |> Option.map (fun cid -> { CID = cid })
                Name = item.name
                Description = item.description
            }

        let asyncGetRecord handler pds did rkey =
            XRPC.Com.Atproto.Repo.asyncGetRecord handler pds did "site.standard.publication" rkey sample
            |> Async.toRecordAbstraction translate

        let asyncFindNewestRecords handler pds did =
            findNewestItems handler pds did "site.standard.publication" sample
            |> AsyncSeq.toRecordAbstractions translate

    module Document =
        let private sample = {|
            site = ""
            path = Some ""
            title = ""
            description = Some ""
            textContent = Some ""
            tags = Some []
            publishedAt = DateTimeOffset.MinValue
            updatedAt = Some DateTimeOffset.MinValue
        |}

        let private translate item =
            let _ = [item; sample]

            {
                Site =
                    if item.site.StartsWith("at://")
                    then Publication { Raw = item.site }
                    else Loose item.site
                Path = item.path
                Title = item.title
                Description = item.description
                TextContent = item.textContent
                Tags =
                    item.tags
                    |> Option.defaultValue []
                PublishedAt = item.publishedAt
                UpdatedAt = item.updatedAt
            }

        let asyncGetRecord handler pds did rkey =
            XRPC.Com.Atproto.Repo.asyncGetRecord handler pds did "site.standard.document" rkey sample
            |> Async.toRecordAbstraction translate

        let asyncFindNewestRecords handler pds did =
            findNewestItems handler pds did "site.standard.document" sample
            |> AsyncSeq.toRecordAbstractions translate
