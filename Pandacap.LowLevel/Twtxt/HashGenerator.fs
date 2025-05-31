namespace Pandacap.LowLevel.Twtxt

open System
open System.Text
open Blake2Fast
open SimpleBase

module HashGenerator =
    let getDateTimeString (dt: DateTimeOffset) =
        let str = dt.ToString("o")
        if str.Length <> 33 then
            failwith "ToString did not give expected date/time format"

        let a = str.Substring(0, 19)
        let b = str.Substring(27, 6)

        let tz = if b = "+00:00" then "Z" else b

        String.concat "" [a; tz]

    let computeHash (data: byte array) =
        Blake2b.ComputeHash(32, data)

    let getHash (url: string) (twt: Twt) =
        let hashStr =
            String.concat "\n" [
                url
                getDateTimeString(twt.timestamp)
                twt.text
            ]
            |> Encoding.UTF8.GetBytes
            |> computeHash
            |> Base32.Rfc4648.Encode

        hashStr.ToLowerInvariant().Substring(hashStr.Length - 7, 7)
