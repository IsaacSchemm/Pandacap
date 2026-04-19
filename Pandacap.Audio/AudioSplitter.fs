namespace Pandacap.Audio

open System
open System.IO
open System.IO.Compression
open System.Net.Http
open NAudio.MediaFoundation
open NAudio.Wave
open Pandacap.Audio.Models
open Pandacap.Audio.Interfaces

type AudioSplitter(httpClientFactory: IHttpClientFactory) =
    let _ = MediaFoundationApi.Startup()

    let readAudioFile (segmentLength: TimeSpan) (filename: string) = seq {
        use reader = new AudioFileReader(filename)

        let length =
            reader.WaveFormat.SampleRate
            * int segmentLength.TotalSeconds
            * reader.WaveFormat.Channels
            * (reader.WaveFormat.BitsPerSample / 8)

        let mutable finished = false
        while not finished do
            let buffer = Array.zeroCreate<byte> length
            let read = reader.Read(buffer)
            if read = 0 then
                finished <- true
            else
                let stream = new MemoryStream(buffer, 0, read, writable = false)

                yield {
                    new IWaveProvider with
                        override _.Read(buffer, offset, count) = stream.Read(buffer, offset, count)
                        override _.WaveFormat = reader.WaveFormat
                }
    }

    let encodeAudioSegment (format: AudioSplitterOutputAudioFormat) (waveProvider: IWaveProvider) =
        use output = new MemoryStream()

        match format with
        | WMA -> MediaFoundationEncoder.EncodeToWma(waveProvider, output)
        | AAC -> MediaFoundationEncoder.EncodeToAac(waveProvider, output)
        | MP3 -> MediaFoundationEncoder.EncodeToMp3(waveProvider, output)

        output.ToArray()

    let createArchive (format: AudioSplitterOutputArchiveFormat) (outputStream: Stream) (encodedSegments: byte array seq) =
        match format with
        | ZIP ->
            let mutable track = 1

            use archive = new ZipArchive(
                outputStream,
                ZipArchiveMode.Create,
                leaveOpen = false)

            for encodedSegment in encodedSegments do
                let entry = archive.CreateEntry(
                    sprintf "%02d.wma" track,
                    CompressionLevel.NoCompression)

                use zipStream = entry.Open()
                zipStream.Write(encodedSegment)

                track <- track + 1

    interface IAudioSplitter with
        member _.SplitIntoSegmentsAsync(uri, segmentLength, fileFormat, archiveFormat, outputStream, cancellationToken) = task {
            let filename = Array.last uri.Segments

            let tempFile = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}{Path.GetExtension(filename)}")

            try
                do! task {
                    use client = httpClientFactory.CreateClient()

                    use! resp = client.GetAsync(uri, cancellationToken)
                    use! stream = resp.EnsureSuccessStatusCode().Content.ReadAsStreamAsync()
                    use fs = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write)
                    do! stream.CopyToAsync(fs, cancellationToken)
                }

                tempFile
                |> readAudioFile segmentLength
                |> Seq.map (encodeAudioSegment fileFormat)
                |> createArchive archiveFormat outputStream
            finally
                if File.Exists(tempFile) then
                    File.Delete(tempFile)
        }
