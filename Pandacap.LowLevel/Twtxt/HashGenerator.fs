namespace Pandacap.LowLevel.Txt

open System
open System.Text
open Blake2Fast
open SimpleBase

module HashGenerator =
    let GetDateTimeString (dt: DateTimeOffset) =
        let str = dt.ToString("o")
        if str.Length <> 33 then
            failwith "ToString did not give expected date/time format"

        let a = str.Substring(0, 19)
        let b = str.Substring(27, 6)

        let tz = if b = "+00:00" then "Z" else b

        String.concat "" [a; tz]

    let GetHash (metadata: Metadata) (twt: Twt) =
        String.concat "\n" [
            metadata.url.Head.OriginalString
            GetDateTimeString(twt.timestamp)
            twt.text
        ]
        |> Encoding.UTF8.GetBytes
        |> Blake2b.ComputeHash
        |> Base32.Rfc4648.Encode
        |> Hash
