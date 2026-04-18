Imports System.Threading

Public Interface IPostCreator
    Function CreatePostAsync(post As INewPost,
                             cancellationToken As CancellationToken) As Task(Of Guid)
End Interface
