namespace Pandacap.Clients

open System
open System.Net.Http
open System.Text.Json

type FurryNetworkClient(factory: IHttpClientFactory) =
    let createClient () = factory.CreateClient()

    let getAsync (url: string) (client: HttpClient) =
        client.GetAsync(url)

    let readAsAsync (_: 'T) (content: HttpContent) = task {
        let! json = content.ReadAsStringAsync()
        return JsonSerializer.Deserialize<'T>(json)
    }

    member _.GetProfileAsync (name: string) = task {
        use client = createClient ()
        use! resp = client |> getAsync $"https://furrynetwork.com/api/character/{Uri.EscapeDataString(name)}/profile"
        return! resp.EnsureSuccessStatusCode().Content |> readAsAsync {|
            customFields = [{|
                key = ""
                value = ""
            |}]
        |}
    }

    member _.GetPromotesAsync (name: string) (size: int) (from: int) = task {
        use client = createClient ()
        use! resp = client |> getAsync $"https://furrynetwork.com/api/search/{Uri.EscapeDataString(name)}/promotes?size={size}&from={from}"
        return! resp.EnsureSuccessStatusCode().Content |> readAsAsync {|
            hits = [{|
                _source = {|
                    title = ""
                    url = ""
                    rating = 0 // 0
                    status = "" // public
                    character = {|
                        name = ""
                        display_name = ""
                        ``private`` = false
                        noindex = false
                        avatar = ""
                        avatar_explicit = 0
                        avatars = {|
                            avatar = ""
                            original = ""
                            small = ""
                            tiny = ""
                        |}
                    |}
                    tags = [""]
                    images = Some {|
                        small = ""
                    |}
                |}
            |}]
            total = 0
        |}
    }
