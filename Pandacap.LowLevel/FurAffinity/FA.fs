namespace Pandacap.FurAffinity

open System
open System.Net.Http
open System.Text.Json
open FSharp.Data

module FA =
    type Rating =
    | General = 0
    | Adult = 1
    | Mature = 2

    type ArtworkMetadata = {
        title: string
        message: string
        keywords: string list
        cat: int
        scrap: bool
        atype: int
        species: int
        gender: int
        rating: Rating
        lock_comments: bool
        folder_ids: Set<int64>
    }

    let private handler = lazy new SocketsHttpHandler(UseCookies = false, PooledConnectionLifetime = TimeSpan.FromMinutes(5L))

    type Domain =
    | WWW
    | SFW

    let private getClient (credentials: IFurAffinityCredentials) (domain: Domain) =
        let client = new HttpClient(handler.Value, disposeHandler = false)
        client.BaseAddress <-
            match domain with
            | WWW -> new Uri("https://www.furaffinity.net/")
            | SFW -> new Uri("https://sfw.furaffinity.net/")
        client.DefaultRequestHeaders.Add("Cookie", $"a={credentials.A}; b={credentials.B}")
        client.DefaultRequestHeaders.UserAgent.ParseAdd(credentials.UserAgent)
        client

    let WhoamiAsync credentials cancellationToken = task {
        use client = getClient credentials WWW
        use! resp = client.GetAsync("/help/", cancellationToken = cancellationToken)
        ignore (resp.EnsureSuccessStatusCode())

        let! html = resp.Content.ReadAsStringAsync(cancellationToken)
        let document = HtmlDocument.Parse html
        return String.concat " / " [
            for item in document.CssSelect("#my-username") do
                item.InnerText().Trim().TrimStart('~')
        ]
    }

    let GetTimeZoneAsync credentials cancellationToken = task {
        use client = getClient credentials WWW
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

    module private Scraper =
        let getName node =
            node
            |> HtmlNode.innerText

        let getValue node =
            node
            |> HtmlNode.tryGetAttribute "value"
            |> Option.map (fun a -> HtmlAttribute.value a)
            |> Option.defaultValue (getName node)

        let getDescendants node =
            HtmlNode.descendants false (fun _ -> true) node

        let getPostOptions (selector: string) (document: HtmlDocument) = [
            for select in document.CssSelect selector do
                for x in getDescendants select do
                    match (HtmlNode.name x).ToLowerInvariant() with
                    | "option" ->
                        {| Value = getValue x; Name = getName x |}
                    | "optgroup" ->
                        for y in getDescendants x do
                            {| Value = getValue y; Name = getName y |}
                    | _ -> ()
        ]

    let ListPostOptionsAsync credentials cancellationToken = task {
        use client = getClient credentials WWW
        use! resp = client.GetAsync("/browse/", cancellationToken = cancellationToken)
        ignore (resp.EnsureSuccessStatusCode())

        let! html = resp.Content.ReadAsStringAsync(cancellationToken)
        let document = HtmlDocument.Parse(html)

        return {|
            Categories = document |> Scraper.getPostOptions "select[name=cat]"
            Types = document |> Scraper.getPostOptions "select[name=atype]"
            Species = document |> Scraper.getPostOptions "select[name=species]"
            Genders = document |> Scraper.getPostOptions "select[name=gender]"
        |}
    }

    let ListGalleryFoldersAsync credentials cancellationToken = task {
        use client = getClient credentials WWW
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
                    {| FolderId = s; Name = HtmlNode.innerText link |}
                | None -> ()
        ]
    }

    type private MultipartSegmentValue =
    | FieldPart of string
    | FilePart of byte[]

    let private multipart segments =
        let content = new MultipartFormDataContent()
        for segment in segments do
            match segment with
            | name, FieldPart value -> content.Add(new StringContent(value), name)
            | name, FilePart data -> content.Add(new ByteArrayContent(data), name, "image.dat")
        content

    let private extractAuthenticityToken (formName: string) (html: HtmlDocument) =
        let m =
            html.CssSelect($"form[name={formName}] input[name=key]")
            |> Seq.map (fun e -> e.AttributeValue("value"))
            |> Seq.tryHead
        match m with
            | Some token -> token
            | None -> failwith $"Form \"{formName}\" with hidden input \"key\" not found in HTML from server"

    let PostArtworkAsync credentials file (metadata: ArtworkMetadata) cancellationToken = task {
        use client = getClient credentials WWW

        let! artwork_submission_page_key = task {
            use! resp = client.GetAsync("/submit/", cancellationToken = cancellationToken)
            ignore (resp.EnsureSuccessStatusCode())
            let! html = resp.Content.ReadAsStringAsync(cancellationToken)
            let token = html |> HtmlDocument.Parse |> extractAuthenticityToken "myform"
            return token
        }

        let! finalize_submission_page_key = task {
            use req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/submit/upload")
            req.Content <- multipart [
                "key", FieldPart artwork_submission_page_key
                "submission_type", FieldPart "submission"
                "submission", FilePart file
            ]

            use! resp = client.SendAsync(req, cancellationToken)
            ignore (resp.EnsureSuccessStatusCode())
            let! html = resp.Content.ReadAsStringAsync(cancellationToken)
            if html.Contains "Security code missing or invalid." then
                failwith "Security code missing or invalid for page"
            return html |> HtmlDocument.Parse |> extractAuthenticityToken "myform"
        }

        return! task {
            use req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/submit/finalize/")
            req.Content <- multipart [
                "part", FieldPart "5"
                "key", FieldPart finalize_submission_page_key
                "submission_type", FieldPart "submission"
                "cat_duplicate", FieldPart ""
                "title", FieldPart metadata.title
                "message", FieldPart metadata.message
                "keywords", FieldPart (metadata.keywords |> Seq.map (fun s -> s.Replace(' ', '_')) |> String.concat " ")
                "cat", FieldPart (metadata.cat.ToString("d"))
                "atype", FieldPart (metadata.atype.ToString("d"))
                "species", FieldPart (metadata.species.ToString("d"))
                "gender", FieldPart (metadata.gender.ToString("d"))
                "rating", FieldPart (metadata.rating.ToString("d"))
                if metadata.scrap then
                    "scrap", FieldPart "1"
                if metadata.lock_comments then
                    "lock_comments", FieldPart "on"
                for id in metadata.folder_ids do
                    "folder_ids[]", FieldPart $"{id}"
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

    type SubmissionDataElement = {
        avatar_mtime: string
        description: string
        lower: string
        title: string
        username: string
    } with
        member this.AvatarUrl = $"https://a.furaffinity.net/{Uri.EscapeDataString(this.avatar_mtime)}/{Uri.EscapeDataString(this.lower)}.gif"

    type Submission = {
        id: int
        fav_id: int64
        submission_data: SubmissionDataElement
        title: string
        thumbnail: string
    }

    [<RequireQualifiedAccess>]
    type FavoritesPage =
    | First
    | After of int64
    | Before of int64

    let GetFavoritesAsync credentials (name: string) domain pagination cancellationToken = task {
        let path =
            match pagination with
            | FavoritesPage.First -> $"/favorites/{Uri.EscapeDataString(name)}"
            | FavoritesPage.After fav_id -> $"/favorites/{Uri.EscapeDataString(name)}/{fav_id}/next"
            | FavoritesPage.Before fav_id -> $"/favorites/{Uri.EscapeDataString(name)}/{fav_id}/prev"

        use client = getClient credentials domain
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

    [<RequireQualifiedAccess>]
    type SubmissionsPage = FromOldest of int64

    let GetSubmissionsAsync credentials domain pagination cancellationToken = task {
        let path =
            match pagination with
            | SubmissionsPage.FromOldest sid -> $"/msg/submissions/old~{sid}@48/"

        use client = getClient credentials domain
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

    type Note = {
        note_id: int
        subject: string
        userDisplayName: string
        time: DateTimeOffset
    }

    let GetNotesAsync credentials cancellationToken = task {
        use client = getClient credentials WWW
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

    type Notification = {
        time: DateTimeOffset
        text: string
    }

    let GetNotificationsAsync credentials cancellationToken = task {
        use client = getClient credentials WWW
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
                | Some t -> {
                    text = HtmlNode.innerText item
                    time = t
                }
        ]
    }
