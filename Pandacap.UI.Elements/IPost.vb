Imports Pandacap.UI.Badges

''' <summary>
''' A post that can be shown in one of Pandacap's "paged" areas, like the gallery or inbox.
''' </summary>
Public Interface IPost
    ''' <summary>
    ''' A badge that shows the origin of a remote post.
    ''' </summary>
    ReadOnly Property Badge As Badge

    ''' <summary>
    ''' The title to be shown for this post in a paged view.
    ''' </summary>
    ReadOnly Property DisplayTitle As String

    ''' <summary>
    ''' An opaque ID for this post; used in pagination.
    ''' </summary>
    ReadOnly Property Id As String

    ''' <summary>
    ''' A URL where the Pandacap administrator can view this content.
    ''' </summary>
    ReadOnly Property InternalUrl As String

    ''' <summary>
    ''' A URL where unauthenticated users can view this content.
    ''' </summary>
    ReadOnly Property ExternalUrl As String

    ''' <summary>
    ''' The date/time at which this content was posted or added.
    ''' </summary>
    ReadOnly Property PostedAt As DateTimeOffset

    ''' <summary>
    ''' The URL of the profile page of the user who posted this content, if any.
    ''' </summary>
    ReadOnly Property ProfileUrl As String

    ''' <summary>
    ''' A list of thumbnails associated with this content. Can be an empty list.
    ''' </summary>
    ReadOnly Property Thumbnails As IEnumerable(Of IPostThumbnail)

    ''' <summary>
    ''' The name of the user who posted this content.
    ''' </summary>
    ReadOnly Property Username As String

    ''' <summary>
    ''' An avatar associated with the user who posted this content, if any.
    ''' </summary>
    ReadOnly Property Usericon As String
End Interface
