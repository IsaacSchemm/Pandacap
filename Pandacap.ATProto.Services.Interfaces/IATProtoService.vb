Imports System.Threading
Imports Microsoft.FSharp.Collections

Public Interface IATProtoService
    Function GetBlobAsync(pds As String,
                          did As String,
                          cid As String,
                          cancellationToken As CancellationToken) As Task(Of IATProtoBlob)

    Function GetCollectionsInRepoAsync(pds As String,
                                       did As String,
                                       cancellationToken As CancellationToken) As Task(Of FSharpList(Of String))

    Function GetLastCommitCIDAsync(pds As String,
                                   did As String,
                                   cancellationToken As CancellationToken) As Task(Of String)

    Function GetRecordCreationTimeAsync(pds As String,
                                        did As String,
                                        collection As String,
                                        recordKey As String,
                                        cancellationToken As CancellationToken) As Task(Of DateTimeOffset?)
End Interface
