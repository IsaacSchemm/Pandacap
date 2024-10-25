namespace Pandacap.LowLevel

open Pandacap.Data

type ListPage = {
    Current: IPost list
    Next: IPost option
}
