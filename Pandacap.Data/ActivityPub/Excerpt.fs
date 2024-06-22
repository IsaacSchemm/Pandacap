namespace Pandacap.Data

open System.IO

/// Allows Pandacap to derive a plaintext excerpt from an HTML string.
module internal Excerpt =
    let private converter = lazy new Textify.HtmlToTextConverter()

    let private getLines (str: string) = seq {
        use sr = new StringReader(str)
        let mutable line = sr.ReadLine()
        while not (isNull line) do
            line
            line <- sr.ReadLine()
        }

    /// Derives a plaintext excerpt from an HTML string.
    let compute (html: string) =
        html
        |> Option.ofObj
        |> Option.defaultValue ""
        |> converter.Value.Convert
        |> getLines
        |> Seq.tryHead
        |> Option.map (fun e ->
            if e.Length > 60
            then $"{e.Substring(0, 60)}..."
            else e)
