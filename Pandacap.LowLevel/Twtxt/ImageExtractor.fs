namespace Pandacap.LowLevel.Twtxt

open System.Text.RegularExpressions

module ImageExtractor =
    let private expression = new Regex("!\[([^\]]*)\]\(([^ \)]+)")

    let FromMarkdown (text: string): Link list = [
        let matches = expression.Matches(text)
        for i in 0 .. matches.Count - 1 do {
            text = matches[i].Groups[1].Value
            url = matches[i].Groups[2].Value
        }
    ]
