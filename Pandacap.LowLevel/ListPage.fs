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

    let Create (seq: 'T seq, size: int) =
        let reverse = seq |> Seq.rev |> Seq.toList
        match reverse with
        | [] ->
            Empty
        | list when list.Length <= size ->
            {
                DisplayList = List.rev list
                Next = None
            }
        | next::this ->
            {
                DisplayList = List.rev this
                Next = Some next
            }
