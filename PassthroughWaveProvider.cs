using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace LocalAudioLinkTakeover
{
    internal class PassthroughWaveProvider : IWaveProvider
    {
        private WaveFormat waveFormat;
        private int bytesPerSample;
        private IWaveProvider input = null;

        public PassthroughWaveProvider()
        {
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            this.bytesPerSample = 4;
        }

        public void SetInput(IWaveProvider input)
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

        public int Read(byte[] buffer, int offset, int count)
        {
            if (count % bytesPerSample != 0)
            {
                throw new ArgumentException("Must read an whole number of samples", "count");
            }

            // blank the buffer
            Array.Clear(buffer, offset, count);

            // read a much data as we can, the rest will be filled with zeroes
            lock (input)
            {
                if (input != null)
                {
                    input.Read(buffer, 0, count);
                }
            }

            return count;
        }


        public WaveFormat WaveFormat
        {
            get { return this.waveFormat; }
        }
    }
}
