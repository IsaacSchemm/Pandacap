namespace Pandacap.UI.Badges

open System

type Badge = {
    Platform: Platform
    Text: string
    Background: string
    Color: string
} with
    member this.WithText(text) = { this with Text = text }

    member this.WithHostFromUriString(url) =
        match Uri.TryCreate(url, UriKind.Absolute) with
        | true, uri -> { this with Text = uri.Host }
        | false, _ -> this
