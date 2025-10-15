namespace Pandacap.LowLevel

open System
open System.Text.Json

module JsonFeed =
    let Parse (json: string) = List.last [
        {|
            version = ""
            title = ""
            home_page_url = ""
            feed_url = ""
            description = ""
            next_url = ""
            icon = ""
            favicon = ""
            authors = [{|
                name = ""
                url = ""
                avatar = ""
            |}]
            language = ""
            expired = Nullable false
            items = [{|
                id = ""
                url = ""
                title = ""
                content_html = ""
                content_text = ""
                summary = ""
                image = ""
                banner_image = ""
                date_published = Nullable DateTimeOffset.MinValue
                date_modified = Nullable DateTimeOffset.MinValue
                authors = [{|
                    name = ""
                    url = ""
                    avatar = ""
                |}]
                tags = [""]
                language = ""
                attachments = [{|
                    url = ""
                    mime_type = ""
                    title = ""
                    size_in_bytes = 0L
                    duration_in_seconds = 0.0
                |}]
            |}]
        |}

        JsonSerializer.Deserialize(json)
    ]
