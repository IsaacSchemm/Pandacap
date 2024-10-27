namespace Pandacap.LowLevel

open System

module BridgyFed =
    let Enabled = true

    let Follower = "https://bsky.brid.gy/bsky.brid.gy"

    let OwnsInbox (url: string) =
        match Uri.TryCreate(url, UriKind.Absolute) with
        | false, _ -> false
        | true, uri -> $".{uri.Host}".EndsWith($".brid.gy")
