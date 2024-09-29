namespace Pandacap.Data

type Badge = {
    Text: string
    Background: string
    Color: string
}

module Badge =
    let Create text bgcolor color = {
        Text = text
        Background = bgcolor
        Color = color
    }
