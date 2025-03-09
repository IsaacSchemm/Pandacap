using NAudio.Wave;

namespace Pandacap.Podcasts
{
    public class WaveProvider(Stream stream, WaveFormat waveFormat) : IWaveProvider
    {
        public WaveFormat WaveFormat =>
            waveFormat;

        public int Read(byte[] buffer, int offset, int count) =>
            stream.Read(buffer, offset, count);
    }
}
