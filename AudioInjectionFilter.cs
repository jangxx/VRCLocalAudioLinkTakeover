using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using UnhollowerBaseLib;
using CSCore;

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
            float[] readBuffer = new float[data.Length];
            int readSamples = this.waveProvider.Read(readBuffer, 0, data.Length);

            for (int i = 0; i < readSamples; i++)
            {
                data[i] = readBuffer[i];
            }
        }
    }
}
