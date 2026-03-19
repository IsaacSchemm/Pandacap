namespace Pandacap.FurAffinity

open System
open System.Net.Http
open System.Text.Json
open FSharp.Data
open Pandacap.FurAffinity.Models
open Pandacap.FurAffinity.Interfaces

type internal FurAffinityClient(
    httpMessageHandler: HttpMessageHandler,
    domain: Domain,
    credentials: IFurAffinityCredentials
) =
    let client =
        let client = new HttpClient(httpMessageHandler, disposeHandler = false)
        client.BaseAddress <-
            match domain with
            | WWW -> new Uri("https://www.furaffinity.net/")
            | SFW -> new Uri("https://sfw.furaffinity.net/")
        client.DefaultRequestHeaders.Add("Cookie", $"a={credentials.A}; b={credentials.B}")
        client.DefaultRequestHeaders.UserAgent.ParseAdd(credentials.UserAgent)
        client

    interface IFurAffinityClient with
        member _.WhoamiAsync(cancellationToken) = task {
            use! resp = client.GetAsync("/help/", cancellationToken = cancellationToken)
            ignore (resp.EnsureSuccessStatusCode())

            let! html = resp.Content.ReadAsStringAsync(cancellationToken)
            let document = HtmlDocument.Parse html
            return String.concat " / " [
                for item in document.CssSelect("#my-username") do
                    item.InnerText().Trim().TrimStart('~')
            ]
        }

        member _.GetTimeZoneAsync(cancellationToken) = task {
            use! resp = client.GetAsync("/controls/settings/", cancellationToken = cancellationToken)

            let! html = resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken)
            let document = HtmlDocument.Parse(html)

            let upstreamTimeZone = {|
                Name =
                    document.CssSelect("select[name=timezone] option[selected]")
                    |> Seq.map (fun option -> option.InnerText())
                    |> Seq.head
                DaylightSaving =
                    document.CssSelect("input[name=timezone_dst]")
                    |> Seq.exists (fun checkbox -> checkbox.TryGetAttribute("checked") |> Option.isSome)
            |}

            return Seq.head (seq {
                for tz in TimeZoneInfo.GetSystemTimeZones() do
                    if upstreamTimeZone.Name.EndsWith(tz.Id) then
                        match upstreamTimeZone.DaylightSaving, tz.SupportsDaylightSavingTime with
                        | true, true -> tz
                        | false, false -> tz
                        | false, true -> TimeZoneInfo.CreateCustomTimeZone(
                            upstreamTimeZone.Name,
                            tz.BaseUtcOffset,
                            upstreamTimeZone.Name,
                            upstreamTimeZone.Name)
                        | true, false -> ()

                TimeZoneInfo.Utc
            })
        }

        member _.ListPostOptionsAsync(cancellationToken) = task {
            use! resp = client.GetAsync("/browse/", cancellationToken = cancellationToken)
            ignore (resp.EnsureSuccessStatusCode())

            let! html = resp.Content.ReadAsStringAsync(cancellationToken)
            let document = HtmlDocument.Parse(html)

            return {
                Categories = document |> Scraper.getPostOptions "select[name=cat]"
                Types = document |> Scraper.getPostOptions "select[name=atype]"
                Species = document |> Scraper.getPostOptions "select[name=species]"
                Genders = document |> Scraper.getPostOptions "select[name=gender]"
            }
        }

        member _.ListGalleryFoldersAsync(cancellationToken) = task {
            use! resp = client.GetAsync("/controls/folders/submissions/", cancellationToken = cancellationToken)
            ignore (resp.EnsureSuccessStatusCode())

            let! html = resp.Content.ReadAsStringAsync(cancellationToken)
            let document = HtmlDocument.Parse html

            let regex = new System.Text.RegularExpressions.Regex("^/gallery/[^/]+/folder/([0-9]+)/")
            let extractId href =
                let m = regex.Match href
                if m.Success then Some (Int64.Parse m.Groups[1].Value) else None

            return [
                for link in document.CssSelect "a" do
                    let id =
                        link.TryGetAttribute "href"
                        |> Option.map (fun a -> HtmlAttribute.value a)
                        |> Option.bind extractId
                    match id with
                    | Some s ->
                        { FolderId = s; Name = HtmlNode.innerText link }
                    | None -> ()
            ]
        }

        member _.PostArtworkAsync(file, metadata, cancellationToken) = task {
            let! artwork_submission_page_key = task {
                use! resp = client.GetAsync("/submit/", cancellationToken = cancellationToken)
                ignore (resp.EnsureSuccessStatusCode())
                let! html = resp.Content.ReadAsStringAsync(cancellationToken)
                let token = html |> HtmlDocument.Parse |> Scraper.extractAuthenticityToken "/submit/upload"
                return token
            }

            let! finalize_submission_page_key = task {
                use req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/submit/upload")
                req.Content <- Multipart.from [
                    "key", Multipart.Field artwork_submission_page_key
                    "submission_type", Multipart.Field "submission"
                    "submission", Multipart.File file
                ]

                use! resp = client.SendAsync(req, cancellationToken)
                ignore (resp.EnsureSuccessStatusCode())
                let! html = resp.Content.ReadAsStringAsync(cancellationToken)
                if html.Contains "Security code missing or invalid." then
                    failwith "Security code missing or invalid for page"
                return html |> HtmlDocument.Parse |> Scraper.extractAuthenticityToken "/submit/finalize/"
            }

            return! task {
                use req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/submit/finalize/")
                req.Content <- Multipart.from [
                    "part", Multipart.Field "5"
                    "key", Multipart.Field finalize_submission_page_key
                    "submission_type", Multipart.Field "submission"
                    "cat_duplicate", Multipart.Field ""
                    "title", Multipart.Field metadata.title
                    "message", Multipart.Field metadata.message
                    "keywords", Multipart.Field (metadata.keywords |> Seq.map (fun s -> s.Replace(' ', '_')) |> String.concat " ")
                    "cat", Multipart.Field (metadata.cat.ToString("d"))
                    "atype", Multipart.Field (metadata.atype.ToString("d"))
                    "species", Multipart.Field (metadata.species.ToString("d"))
                    "gender", Multipart.Field (metadata.gender.ToString("d"))
                    "rating", Multipart.Field (metadata.rating.ToString("d"))
                    if metadata.scrap then
                        "scrap", Multipart.Field "1"
                    if metadata.lock_comments then
                        "lock_comments", Multipart.Field "on"
                    for id in metadata.folder_ids do
                        "folder_ids[]", Multipart.Field $"{id}"
                ]
                use! resp = client.SendAsync(req, cancellationToken)
                let! html = resp.Content.ReadAsStringAsync(cancellationToken)
                if html.Contains "Security code missing or invalid." then
                    failwith "Security code missing or invalid for page"

                let redirectMessages =
                    HtmlDocument.Parse(html).CssSelect(".redirect-message")
                    |> Seq.map (fun node -> node.InnerText())
                    |> Seq.toList

                if redirectMessages <> [] then
                    failwithf "Could not post to Fur Affinity: %A" redirectMessages

                return resp.RequestMessage.RequestUri
            }
        }

        member _.GetFavoritesAsync(name, pagination, cancellationToken) = task {
            let path =
                match pagination with
                | FavoritesPage.First -> $"/favorites/{Uri.EscapeDataString(name)}"
                | FavoritesPage.After fav_id -> $"/favorites/{Uri.EscapeDataString(name)}/{fav_id}/next"
                | FavoritesPage.Before fav_id -> $"/favorites/{Uri.EscapeDataString(name)}/{fav_id}/prev"

            use! resp = client.GetAsync(path, cancellationToken = cancellationToken)

            let! html = resp.Content.ReadAsStringAsync(cancellationToken)

            let subset = html.Substring(List.max [0; html.IndexOf("""<div id="standardpage">""")])

            let document = HtmlDocument.Parse(subset)

            let submissionData =
                document.CssSelect("#js-submissionData")
                |> Seq.map (fun node -> node.InnerText())
                |> Seq.map (fun json -> JsonSerializer.Deserialize<Map<int, SubmissionDataElement>>(json))
                |> Seq.tryHead
                |> Option.defaultValue Map.empty

            return
                seq {
                    for sid, submissionDataElement in Map.toSeq submissionData do
                        match document.CssSelect($"#sid-{sid}") |> Seq.tryHead with
                        | None -> ()
                        | Some figure -> {
                            id = sid
                            fav_id = figure.Attribute("data-fav-id").Value() |> int64
                            submission_data = submissionDataElement
                            title = figure.CssSelect("figcaption p a").Head.InnerText()
                            thumbnail = figure.CssSelect("img").Head.Attribute("src").Value()
                        }
                }
                |> Seq.sortByDescending (fun fav -> fav.fav_id)
                |> Seq.toList
        }

        member _.GetSubmissionsAsync(pagination, cancellationToken) = task {
            let path =
                match pagination with
                | SubmissionsPage.FromOldest sid -> $"/msg/submissions/old~{sid}@48/"

            use! resp = client.GetAsync(path, cancellationToken = cancellationToken)

            let! html = resp.Content.ReadAsStringAsync(cancellationToken)

            let subset = html.Substring(List.max [0; html.IndexOf("""<div id="standardpage">""")])

            let document = HtmlDocument.Parse(subset)

            let submissionData =
                document.CssSelect("#js-submissionData")
                |> Seq.map (fun node -> node.InnerText())
                |> Seq.map (fun json -> JsonSerializer.Deserialize<Map<int, SubmissionDataElement>>(json))
                |> Seq.tryHead
                |> Option.defaultValue Map.empty

            return [
                for sid, submissionDataElement in Map.toSeq submissionData do
                    match document.CssSelect($"#sid-{sid}") |> Seq.tryHead with
                    | None -> ()
                    | Some figure -> {
                        id = sid
                        fav_id = 0L
                        submission_data = submissionDataElement
                        title = figure.CssSelect("figcaption p a").Head.InnerText()
                        thumbnail = figure.CssSelect("img").Head.Attribute("src").Value()
                    }
            ]
        }

        member _.GetNotesAsync(cancellationToken) = task {
            use! resp = client.GetAsync("/msg/pms", cancellationToken = cancellationToken)

            let! html = resp.Content.ReadAsStringAsync(cancellationToken)

            let document = HtmlDocument.Parse(html)

            return [
                for noteListItem in document.CssSelect("#notes-list tr.note") @ document.CssSelect("#notes-list .note-list-container") do {
                    note_id =
                        noteListItem.CssSelect("input[type=checkbox]")
                        |> Seq.exactlyOne
                        |> HtmlNode.tryGetAttribute "value"
                        |> Option.get
                        |> HtmlAttribute.value
                        |> int
                    subject =
                        noteListItem.CssSelect("a.notelink")
                        |> Seq.exactlyOne
                        |> HtmlNode.innerText
                    userDisplayName =
                        noteListItem.CssSelect(".js-displayName")
                        |> Seq.tryHead
                        |> Option.map HtmlNode.innerText
                        |> Option.toObj
                    time =
                        noteListItem.CssSelect("span[data-time]")
                        |> Seq.exactlyOne
                        |> HtmlNode.tryGetAttribute "data-time"
                        |> Option.get
                        |> HtmlAttribute.value
                        |> int64
                        |> DateTimeOffset.FromUnixTimeSeconds
                }
            ]
        }

        member _.GetJournalAsync(journalId, cancellationToken) = task {
            use! resp = client.GetAsync($"/journal/{journalId}/", cancellationToken = cancellationToken)

            let! html = resp.Content.ReadAsStringAsync(cancellationToken)

            let document = HtmlDocument.Parse(html)

            let metaTags = seq {
                for meta in document.CssSelect("meta") do
                    let property =
                        meta
                        |> HtmlNode.tryGetAttribute "property"
                        |> Option.map (HtmlAttribute.value)
                    let content =
                        meta
                        |> HtmlNode.tryGetAttribute "content"
                        |> Option.map (HtmlAttribute.value)
                    match property, content with
                    | Some p, Some c -> {| property = p; content = c|}
                    | _ -> ()
            }

            return {
                title =
                    metaTags
                    |> Seq.where (fun meta -> meta.property = "og:title")
                    |> Seq.map (fun meta -> meta.content)
                    |> Seq.tryHead
                    |> Option.defaultValue $"{journalId}"
                url =
                    metaTags
                    |> Seq.where (fun meta -> meta.property = "og:url")
                    |> Seq.map (fun meta -> meta.content)
                    |> Seq.tryHead
                    |> Option.defaultValue resp.RequestMessage.RequestUri.AbsoluteUri
                avatar =
                    metaTags
                    |> Seq.where (fun meta -> meta.property = "og:image")
                    |> Seq.map (fun meta -> meta.content)
                    |> Seq.tryHead
                    |> Option.toObj
            }
        }

        member _.GetNotificationsAsync(cancellationToken) = task {
            use! resp = client.GetAsync("/msg/others/", cancellationToken = cancellationToken)

            let! html = resp.Content.ReadAsStringAsync(cancellationToken)

            let document = HtmlDocument.Parse(html)

            return [
                for item in document.CssSelect(".message-stream li") do
                    let time =
                        item.CssSelect("span[data-time]")
                        |> Seq.tryHead
                        |> Option.bind (HtmlNode.tryGetAttribute "data-time")
                        |> Option.map (HtmlAttribute.value >> int64 >> DateTimeOffset.FromUnixTimeSeconds)
                    match time with
                    | None -> ()
                    | Some t ->
                        let journalId =
                            item.CssSelect("input[type=checkbox]")
                            |> Seq.map (fun node -> {|
                                name =
                                    node
                                    |> HtmlNode.tryGetAttribute "name"
                                    |> Option.map HtmlAttribute.value
                                    |> Option.defaultValue ""
                                value =
                                    node
                                    |> HtmlNode.tryGetAttribute "value"
                                    |> Option.map HtmlAttribute.value
                                    |> Option.defaultValue ""
                            |})
                            |> Seq.where (fun x -> x.name = "journals[]")
                            |> Seq.choose (fun x ->
                                match Int32.TryParse(x.value) with
                                | true, v -> Some v
                                | false, _ -> None)
                            |> Seq.tryHead

                        {
                            text = HtmlNode.innerText item
                            time = t
                            journalId = Option.toNullable journalId
                        }
            ]
        }

        member _.PostJournalAsync(subject, message, rating, cancellationToken) = task {
            let! journal_submission_page_key = task {
                use! resp = client.GetAsync("/controls/journal/", cancellationToken = cancellationToken)
                ignore (resp.EnsureSuccessStatusCode())
                let! html = resp.Content.ReadAsStringAsync(cancellationToken)
                let token = html |> HtmlDocument.Parse |> Scraper.extractAuthenticityToken "/controls/journal/"
                return token
            }

            use req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/controls/journal/")
            req.Content <- new FormUrlEncodedContent(dict [
                "id", "0"
                "key", journal_submission_page_key
                "do", "update"

                "subject", subject
                "rating", rating.ToString("d")
                "message", message
            ])

            use! resp = client.SendAsync(req, cancellationToken = cancellationToken)
            let! html = resp.Content.ReadAsStringAsync(cancellationToken)
            if html.Contains "Security code missing or invalid." then
                failwith "Security code missing or invalid for page"

            let redirectMessages =
                HtmlDocument.Parse(html).CssSelect(".redirect-message")
                |> Seq.map (fun node -> node.InnerText())
                |> Seq.toList

            if redirectMessages <> [] then
                failwithf "Could not post to Fur Affinity: %A" redirectMessages

            return resp.RequestMessage.RequestUri
        }
