using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;
using MelonLoader;

namespace LocalAudioLinkTakeover
{
    internal class PassthroughWaveProvider : IReadableAudioSource<float>
    {
        private MelonLogger.Instance LoggerInstance = new MelonLogger.Instance("Local_AudioLink_Takeover::PassthroughWaveProvider");

        private WaveFormat waveFormat;
        private int bytesPerSample;
        private IReadableAudioSource<byte> input = null;
        private object inputLock = new object();

        private float currentBoost = 1.0f;
        private long samplesSinceLastClip = 0;

        public float MaxBoost { get; set; } = 15.0f;
        public float MinBoost { get; set; } = 1.0f;
        public float BoostIncreaseSpeed { get; set; } = 0.3f;

        public PassthroughWaveProvider()
        {
            this.waveFormat = new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat);
            this.bytesPerSample = 4;
        }

        public void SetInput(IReadableAudioSource<byte> input)
        {
            lock(this.inputLock)
            {
                this.input = input;
            }
        }

        public void ClearInput()
        {
            lock (this.inputLock)
            {
                this.input = null;
            }
        }

        unsafe public int Read(float[] buffer, int offset, int count)
        {
            // blank the buffer
            Array.Clear(buffer, offset, count);

            // read a much data as we can, the rest will be filled with zeroes
            lock (this.inputLock)
            {
                if (this.input != null)
                {
                    byte[] readBuffer = new byte[count * 4];
                    int bytesRead = this.input.Read(readBuffer, 0, count * 4);

                    fixed (byte* p = readBuffer)
                    {
                        float* bufferAsFloat = (float*)p;
                        for (int i = 0; i < bytesRead/4; i++)
                        {
                            float value = *(bufferAsFloat + i);
                            float boosted = value * currentBoost;

                            if (Math.Abs(boosted) > 1.0f)
                            {
                                currentBoost = Math.Max(MinBoost, 1.0f / Math.Abs(value)); // bring boost down to be _just_ at the max
                                value = Math.Sign(value) * 1.0f;
                                samplesSinceLastClip = 0;
                            }
                            else
                            {
                                samplesSinceLastClip++;
                            }

                            buffer[i] = boosted;
                        }
                    }

                    if (samplesSinceLastClip > 48000) // increase boost once a second if we have not clipped since
                    {
                        currentBoost = Math.Min(MaxBoost, currentBoost + BoostIncreaseSpeed);
                        samplesSinceLastClip -= 48000;
                    }

                    LoggerInstance.Msg(currentBoost + " : " + samplesSinceLastClip);
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
