namespace Pandacap.LowLevel

type ListPage<'T> = {
    DisplayList: 'T list
    Next: 'T option
}

module ListPage =
    let Empty = {
        DisplayList = []
        Next = None
    }
