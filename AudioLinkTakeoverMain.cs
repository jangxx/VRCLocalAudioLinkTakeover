using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using VRC;
using NAudio.Wave;
using UnityEngine;
using UnhollowerRuntimeLib;

namespace LocalAudioLinkTakeover
{
    public class AudioLinkTakeoverMain : MelonMod
    {
        private AudioSource emptyAudioSource = null;
        private PassthroughWaveProvider outputWaveProvider = null;

        private WasapiLoopbackCapture na_captureDevice = null;
        private MediaFoundationResampler na_resampler = null;
        private ConfigurableWaveInProvider na_recorder = null;

        public AudioLinkTakeoverMain()
        {
        }

        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<AudioInjectionFilter>();

            this.outputWaveProvider = new PassthroughWaveProvider();

            SetupCapture();
        }

        public void SetupCapture()
        {
            this.outputWaveProvider.ClearInput();

            if (this.na_captureDevice != null)
            {
                this.na_captureDevice.StopRecording();
                this.na_captureDevice.Dispose();
            }

            if (this.na_resampler != null)
            {
                this.na_resampler.Dispose();
                this.na_resampler = null;
            }

            if (this.na_recorder != null)
            {
                this.na_recorder = null;
            }

            this.na_captureDevice = new WasapiLoopbackCapture();
            this.na_recorder = new ConfigurableWaveInProvider(this.na_captureDevice);
            this.na_recorder.BufferLength = this.na_recorder.WaveFormat.BitsPerSample * this.na_recorder.WaveFormat.SampleRate; // take one eight of the sample rate as buffer size

            this.na_resampler = new MediaFoundationResampler(this.na_recorder, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));

            this.outputWaveProvider.SetInput(this.na_resampler);
        }

        //public override void OnUpdate()
        //{
        //}

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            var udonBehaviors = UnityEngine.Object.FindObjectsOfType<VRC.Udon.UdonBehaviour>();

            foreach (var udonBehavior in udonBehaviors)
            {
                if (udonBehavior.name == "AudioLink")
                {
                    LoggerInstance.Msg("Found AudioLink behavior");

                    var variableNames = udonBehavior.publicVariables.VariableSymbols.Cast<Il2CppSystem.Collections.Generic.Dictionary<string, VRC.Udon.Common.Interfaces.IUdonVariable>.KeyCollection>();

                    this.emptyAudioSource = udonBehavior.gameObject.AddComponent<AudioSource>();
                    emptyAudioSource.volume = 0.001f;

                    var filter = udonBehavior.gameObject.AddComponent<AudioInjectionFilter>();
                    filter.enabled = false;

                    bool success = udonBehavior.publicVariables.TrySetVariableValue<AudioSource>("audioSource", emptyAudioSource);

                    LoggerInstance.Msg(success ? "set empty audio source" : "didnt work");
                }
            }
        }
    }
}
