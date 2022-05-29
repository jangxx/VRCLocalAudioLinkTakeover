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

        private const int SAMPLE_RATE = 48000;

        private int sample = 0;

        public AudioInjectionFilter(IntPtr obj0) : base(obj0)
        {

        }

        public void Start()
        {
            LoggerInstance.Msg("AudioInjectionFilter was started");
        }

        public void OnAudioFilterRead(Il2CppStructArray<float> data, int channels)
        {
            //LoggerInstance.Msg("reading " + data.Length + " samples with " + channels + " channels");
            for (int i = 0; i < data.Count; i++)
            {
                float x = (float)this.sample / (float)SAMPLE_RATE;
                data[i] = (float)Math.Sin(x * 440 * Math.PI * 2) * 0.7f;

                sample = (sample + 1) % SAMPLE_RATE;
            }
        }
    }
}
