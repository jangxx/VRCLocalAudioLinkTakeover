using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;

namespace LocalAudioLinkTakeover
{
    internal class PassthroughWaveProvider : IReadableAudioSource<float>
    {
        private WaveFormat waveFormat;
        private int bytesPerSample;
        private IReadableAudioSource<byte> input = null;

        public PassthroughWaveProvider()
        {
            this.waveFormat = new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat);
            this.bytesPerSample = 4;
        }

        public void SetInput(IReadableAudioSource<byte> input)
        {
            lock(this.input)
            {
                this.input = input;
            }
        }

        public void ClearInput()
        {
            if (this.input == null)
            {
                return;
            }

            lock (this.input)
            {
                this.input = null;
            }
        }

        unsafe public int Read(float[] buffer, int offset, int count)
        {
            // blank the buffer
            Array.Clear(buffer, offset, count);

            // read a much data as we can, the rest will be filled with zeroes
            lock (input)
            {
                if (input != null)
                {
                    byte[] readBuffer = new byte[count * 4];
                    int bytesRead = input.Read(readBuffer, 0, count * 4);

                    fixed (byte* p = readBuffer)
                    {
                        float* value = (float*)p;
                        for (int i = 0; i < bytesRead/4; i++)
                        {
                            buffer[i] = *(value + i);
                        }
                    }
                }
            }

            return count;
        }

        public void Dispose()
        {
            // do nothing
        }

        public WaveFormat WaveFormat
        {
            get { return this.waveFormat; }
        }

        public bool CanSeek => false;

        public long Position { get => 0; set { } }

        public long Length => 0;
    }
}
