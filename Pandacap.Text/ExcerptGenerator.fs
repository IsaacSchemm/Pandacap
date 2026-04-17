namespace Pandacap.Text

module ExcerptGenerator =
    let FromText (length: int) (e: string) =
        if isNull e then ""
        else if e.Length > length then $"{e.Substring(0, length - 3)}..."
        else e
