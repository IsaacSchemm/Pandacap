namespace Pandacap.Podcasts

open System
open System.IO
open System.Net.Http
open System.Threading
open NAudio.MediaFoundation
open System.IO.Compression
open NAudio.Wave

type WmaZipSplitter(httpClientFactory: IHttpClientFactory) =
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

    let encodeAudioSegment (waveProvider: IWaveProvider) =
        use output = new MemoryStream()
        MediaFoundationEncoder.EncodeToWma(waveProvider, output)
        output.ToArray()

    let createZip (outputStream: Stream) (encodedSegments: byte array seq) =
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

    member _.SegmentZip(uri: Uri, segmentLength: TimeSpan, outputStream: Stream, cancellationToken: CancellationToken) = task {
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
            |> Seq.map encodeAudioSegment
            |> createZip outputStream
        finally
            if File.Exists(tempFile) then
                File.Delete(tempFile)
    }
