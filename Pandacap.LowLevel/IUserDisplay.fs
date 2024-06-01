namespace Pandacap.LowLevel

/// Contains display information for another user on the "followers" and "following" pages.
type IUserDisplay =
    abstract member Id: string
    abstract member Name: string
    abstract member IconUrl: string
    abstract member Url: string

module UserDisplay =
    let ForUnresolvableActor(id: string) = {
        new IUserDisplay with
            member _.Id = id
            member _.Name = id
            member _.IconUrl = null
            member _.Url = id
    }
