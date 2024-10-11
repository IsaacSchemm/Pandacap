namespace Pandacap.Types

type ListPage<'T> = {
    DisplayList: 'T list
    Next: 'T option
}

module ListPage =
    let Empty = {
        DisplayList = []
        Next = None
    }
