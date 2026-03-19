namespace Pandacap.FurAffinity.Models

type Journal = {
    title: string
    url: string
    avatar: string
} with
    member this.Username =
        this.avatar.Split('/')
        |> Array.last
        |> (fun str -> str.Split('.'))
        |> Array.head
    member this.Profile =
        $"https://www.furaffinity.net/user/{this.Username}/"
