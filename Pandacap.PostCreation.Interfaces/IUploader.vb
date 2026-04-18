Imports System.Threading
Imports Microsoft.AspNetCore.Http

Public Interface IUploader
    Function UploadAndRenderAsync(file As IFormFile,
                                  altText As String,
                                  cancellationToken As CancellationToken) As Task(Of Guid)

    Function DeleteIfExistsAsync(id As Guid,
                                 cancellationToken As CancellationToken) As Task
End Interface
