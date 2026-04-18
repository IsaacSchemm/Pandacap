Imports Pandacap.Database

Public Interface INewPost
    ReadOnly Property PostType As Post.PostType
    ReadOnly Property Title As String
    ReadOnly Property MarkdownBody As String
    ReadOnly Property Tags As String
    ReadOnly Property UploadId As Guid?
    ReadOnly Property LinkUrl As String
    ReadOnly Property AltText As String
    ReadOnly Property FocusTop As Boolean
End Interface
