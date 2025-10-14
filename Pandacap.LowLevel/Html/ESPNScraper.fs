namespace Pandacap.Html

open System
open System.Text.Json

module ESPNScraper =
    let ParseContributorData (json: string) = List.last [
        {|
            page = {|
                content = {|
                    contributor = {|
                        feed = [{|
                            headline = ""
                            id = ""
                            published = DateTimeOffset.MinValue
                            byline = ""
                            description = ""
                            hdr = {|
                                image = {|
                                    url = ""
                                |}
                            |}
                            absoluteLink = ""
                        |}]
                    |}
                |}
            |}
        |}

        JsonSerializer.Deserialize(json)
    ]
