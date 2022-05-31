using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;
using MelonLoader;

namespace LocalAudioLinkTakeover
{
    internal class PassthroughWaveProvider
    {
        private MelonLogger.Instance LoggerInstance = new MelonLogger.Instance("Local_AudioLink_Takeover::PassthroughWaveProvider");

        private IReadableAudioSource<byte> input = null;
        private object inputLock = new object();

        private float currentBoost = 1.0f;
        private long samplesSinceLastClip = 0;

        public float MaxBoost { get; set; } = 15.0f;
        public float MinBoost { get; set; } = 1.0f;
        public float BoostIncreaseSpeed { get; set; } = 0.3f;
        public WaveFormat Format { get; set; } = new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat);

        public PassthroughWaveProvider()
        {
        }

        public void SetInput(IReadableAudioSource<byte> input)
        {
            lock(this.inputLock)
            {
                this.input = input;
                this.currentBoost = MinBoost + (MaxBoost - MinBoost) / 2.0f;
            }
        }

        public void ClearInput()
        {
            lock (this.inputLock)
            {
                this.input = null;
            }
        }

        unsafe public int Read(float[] buffer, int count, int channels)
        {
            // blank the buffer
            Array.Clear(buffer, 0, count);

            int readCount = count / channels * Format.Channels;

            // read a much data as we can, the rest will be filled with zeroes
            lock (this.inputLock)
            {
                if (this.input != null)
                {
                    byte[] readBuffer = new byte[readCount * 4];
                    int bytesRead = this.input.Read(readBuffer, 0, readCount * 4);

                    fixed (byte* p = readBuffer)
                    {
                        float* bufferAsFloat = (float*)p;
                        for (int i = 0; i < bytesRead/4; i += Format.Channels)
                        {
                            for (int c = 0; c < channels; c++)
                            {
                                float value = *(bufferAsFloat + i + Math.Min(c, Format.Channels-1)); // fill the remaining channels with data from the highest channel (not ideal but shouldn't cause problems in this instance)
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

                                buffer[i * channels / Format.Channels + c] = boosted;
                            }
                        }
                    }

                    currentBoost = Math.Min(MaxBoost, currentBoost + BoostIncreaseSpeed * ((float)samplesSinceLastClip / 48000.0f));
                    samplesSinceLastClip = 0;

                    //LoggerInstance.Msg(currentBoost + " : " + samplesSinceLastClip);
                }
            }

            return count;
        }
    }
}
