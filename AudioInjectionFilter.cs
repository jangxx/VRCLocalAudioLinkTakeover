using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using UnhollowerBaseLib;
using NAudio.Wave;

namespace LocalAudioLinkTakeover
{
    internal class AudioInjectionFilter : MonoBehaviour
    {
        private MelonLogger.Instance LoggerInstance = new MelonLogger.Instance("Local_AudioLink_Takeover::AudioInjectionFilter");

        private PassthroughWaveProvider waveProvider;

        private const int SAMPLE_RATE = 48000;

        public AudioInjectionFilter(IntPtr obj0) : base(obj0)
        {
        }

        public void SetInputProvider(PassthroughWaveProvider provider)
        {
            this.waveProvider = provider;
        }

        public void Start()
        {
            LoggerInstance.Msg("AudioInjectionFilter was started");
        }

        unsafe public void OnAudioFilterRead(Il2CppStructArray<float> data, int channels)
        {
            //LoggerInstance.Msg("reading " + data.Length + " samples with " + channels + " channels");
            byte[] readBuffer = new byte[data.Length * 4];
            this.waveProvider.Read(readBuffer, 0, readBuffer.Length);

            fixed (byte* buffer = readBuffer)
            {
                for (int i = 0; i < data.Length; i++) {
                    // do some fast unsafe casts to get our float values back
                    float* value = (float*)(buffer + i * 4);
                    data[i] = *value;
                }
            }
        }
    }
}
