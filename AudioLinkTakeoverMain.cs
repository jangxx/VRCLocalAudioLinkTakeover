using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using VRC;
using CSCore;
using UnityEngine;
using UnhollowerRuntimeLib;

namespace LocalAudioLinkTakeover
{
    public class AudioLinkTakeoverMain : MelonMod
    {
        private AudioSource emptyAudioSource = null;
        private PassthroughWaveProvider outputWaveProvider = null;

        private CSCore.SoundIn.WasapiLoopbackCapture cs_captureDevice = null;
        private CSCore.Streams.SoundInSource cs_recorder = null;

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

            if (this.cs_captureDevice != null)
            {
                this.cs_captureDevice.Stop();
                this.cs_captureDevice.Dispose();
            }

            if (this.cs_recorder != null)
            {
                this.cs_recorder = null;
            }

            this.cs_captureDevice = new CSCore.SoundIn.WasapiLoopbackCapture(50, new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat));
            this.cs_captureDevice.Initialize();

            this.cs_recorder = new CSCore.Streams.SoundInSource(this.cs_captureDevice);

            this.outputWaveProvider.SetInput(this.cs_recorder);
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
