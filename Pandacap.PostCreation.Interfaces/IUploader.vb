Imports System.Threading

Public Interface IUploader
    Function UploadAndRenderAsync(data As Byte(),
                                  contentType As String,
                                  altText As String,
                                  cancellationToken As CancellationToken) As Task(Of Guid)

    Function DeleteIfExistsAsync(id As Guid,
                                 cancellationToken As CancellationToken) As Task
End Interface
