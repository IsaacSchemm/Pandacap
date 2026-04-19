Imports System.IO
Imports System.Threading
Imports Pandacap.Audio.Models

Public Interface IAudioSplitter
    Function SplitIntoSegmentsAsync(uri As Uri,
                                    segmentLength As TimeSpan,
                                    fileFormat As AudioSplitterOutputAudioFormat,
                                    archiveFormat As AudioSplitterOutputArchiveFormat,
                                    output As Stream,
                                    cancellationToken As CancellationToken) As Task
End Interface
