namespace Pandacap.LowLevel.Txt

type Feed = {
    metadata: Metadata
    twts: Twt list
} with
    member this.WithUrl url =
        let m = { this.metadata with url = url }
        { this with metadata = m }

