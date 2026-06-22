Imports Pandacap.ActivityPub.Models.Interfaces
Imports Pandacap.ATProto.Models

Public Interface IATProtoBridge
    ''' <summary>
    ''' Gets the atproto URIs for any bridged Bluesky / atproto versions of an ActivityPub post.
    ''' </summary>
    Function FindBridgedPostsAsync(post As IActivityPubPost) As IAsyncEnumerable(Of ATProtoRefUri)
End Interface
