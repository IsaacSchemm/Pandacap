Imports Pandacap.UI.Badges

''' <summary>
''' A user or feed which the Pandacap admin is following, as shown in the UI.
''' </summary>
Public Interface IFollow
    ''' <summary>
    ''' A badge that shows the origin of a remote user or feed.
    ''' </summary>
    ReadOnly Property Badge As Badge

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
