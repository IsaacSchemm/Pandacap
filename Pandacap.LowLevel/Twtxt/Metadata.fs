namespace Pandacap.LowLevel.Twtxt

open System

type Metadata = {
    url: Uri list
    nick: string list
    avatar: string list
    follow: Link list
    link: Link list
    refresh: int list
}
