Imports Pandacap.Platforms

''' <summary>
''' A user or feed which the Pandacap admin is following, as shown in the UI.
''' </summary>
Public Interface IFollow
    ''' <summary>
    ''' The platform this user or feed originates from. Used to render badges that show the origin of a remote post.
    ''' </summary>
    ReadOnly Property Platform As PostPlatform

    ''' <summary>
    ''' A URL that corresponds to where the post is hosted. Used to render badges that show the origin of a remote post.
    ''' </summary>
    ReadOnly Property Url As String

    ''' <summary>
    ''' A URL where unauthenticated users can view this user or feed, if any.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property LinkUrl As String

    ''' <summary>
    ''' A name for this user or feed.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Username As String

    ''' <summary>
    ''' An avatar associated with this user or feed, if any.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property IconUrl As String

    ''' <summary>
    ''' Whether the content from this user that shows up in Pandacap's inbox is filtered in some way (for example, by hiding reposts).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Filtered As Boolean
End Interface
