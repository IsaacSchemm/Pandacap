Imports System.Threading

Public Interface IBridgedPostLinker
    ''' <summary>
    ''' Checks recent posts in the Pandacap database which do not have links to bridged versions
    ''' (such as a Bluesky post created by Bridgy Fed), and adds those links when found.
    ''' </summary>
    Function LinkAllBridgedPostsAsync(Optional cancellationToken As CancellationToken = Nothing) As Task
End Interface
