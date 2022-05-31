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
        private AudioSource attachedAudioSource = null;
        private AudioSource backupAudioSource = null;
        private AudioInjectionFilter audioInjectionFilter = null;
        private VRC.Udon.UdonBehaviour audioLinkBehavior = null;
        private bool audioLinkTakenOver = false;
        private bool foundAudioLink = false;

        private PassthroughWaveProvider outputWaveProvider = null;

        private CSCore.SoundIn.WasapiCapture cs_captureDevice = null;
        private CSCore.Streams.SoundInSource cs_recorder = null;

        private QuickMenuSettings quickMenuSettings = new QuickMenuSettings();

        public AudioLinkTakeoverMain()
        {
        }

        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<AudioInjectionFilter>();

            this.outputWaveProvider = new PassthroughWaveProvider();

            this.quickMenuSettings.MenuInteractionEvent += OnMenuInteraction;
            this.quickMenuSettings.Init();
        }

        public void OnMenuInteraction(object sender, EventArgs args)
        {
            var menuEventArgs = (MenuInteractionEventArgs)args;

            switch (menuEventArgs.Type)
            {
                case MenuInteractionEventArgs.MenuInteractionType.DISABLE_AUDIOLINK:
                    DisableCapture();
                    EnableTakeover(false);
                    break;
                case MenuInteractionEventArgs.MenuInteractionType.RELEASE_TAKEOVER:
                    DisableCapture();
                    ReleaseAudioLinkTakeover();
                    break;
                case MenuInteractionEventArgs.MenuInteractionType.SET_OUTPUT:
                    SetupCapture(menuEventArgs.deviceParam, false);
                    EnableTakeover(true);
                    break;
                case MenuInteractionEventArgs.MenuInteractionType.SET_INPUT:
                    SetupCapture(menuEventArgs.deviceParam, true);
                    EnableTakeover(true);
                    break;
            }
        }

        public void DisableCapture()
        {
            this.outputWaveProvider.ClearInput();

            if (this.cs_captureDevice != null)
            {
                this.cs_captureDevice.Stop();
                this.cs_captureDevice.Dispose();
                this.cs_captureDevice = null;
            }

            if (this.cs_recorder != null)
            {
                this.cs_recorder = null;
            }
        }

        public void SetupCapture(CSCore.CoreAudioAPI.MMDevice device, bool input)
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

            if (input) // if this is an input device use the normal Wasapi capture
            {
                this.cs_captureDevice = new CSCore.SoundIn.WasapiCapture(true, CSCore.CoreAudioAPI.AudioClientShareMode.Shared, 15, new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat));
            }
            else // otherwise use the loopback capture
            {
                this.cs_captureDevice = new CSCore.SoundIn.WasapiLoopbackCapture(5, new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat));
            }

            LoggerInstance.Msg("channels: " + this.cs_captureDevice.WaveFormat.Channels);

            this.cs_captureDevice.Device = device;
            this.cs_captureDevice.Initialize();

            this.cs_recorder = new CSCore.Streams.SoundInSource(this.cs_captureDevice, 2048*2*32/8);

            this.cs_captureDevice.Start();
            this.outputWaveProvider.SetInput(this.cs_recorder);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            this.foundAudioLink = false;
            this.audioLinkTakenOver = false;

            var udonBehaviors = UnityEngine.Object.FindObjectsOfType<VRC.Udon.UdonBehaviour>();

            foreach (var udonBehavior in udonBehaviors)
            {
                if (udonBehavior.name == "AudioLink")
                {
                    LoggerInstance.Msg("Found AudioLink behavior");
                    this.foundAudioLink = true;

                    //var variableNames = udonBehavior.publicVariables.VariableSymbols.Cast<Il2CppSystem.Collections.Generic.Dictionary<string, VRC.Udon.Common.Interfaces.IUdonVariable>.KeyCollection>();

                    this.audioLinkBehavior = udonBehavior;

                    this.attachedAudioSource = udonBehavior.gameObject.AddComponent<AudioSource>();
                    //this.attachedAudioSource.volume = 0.001f;
                    this.attachedAudioSource.enabled = false;

                    this.audioInjectionFilter = udonBehavior.gameObject.AddComponent<AudioInjectionFilter>();
                    this.audioInjectionFilter.enabled = false;
                    this.audioInjectionFilter.SetInputProvider(this.outputWaveProvider);

                    break;
                }
            }

            this.quickMenuSettings.SetAudioLinkFound(this.foundAudioLink);
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            this.attachedAudioSource = null;
            this.audioInjectionFilter = null;
            this.audioLinkBehavior = null;
            this.backupAudioSource = null;

            this.foundAudioLink = false;
            this.audioLinkTakenOver = false;
        }

        private void ReleaseAudioLinkTakeover()
        {
            if (!this.audioLinkTakenOver || !this.foundAudioLink) return;

            if (this.backupAudioSource == null) return;

            //bool success = this.audioLinkBehavior.publicVariables.TrySetVariableValue<AudioSource>("audioSource", this.backupAudioSource);
            //LoggerInstance.Msg(success ? "Successfully restored AudioLink's AudioSource" : "Could not restore AudioLink's AudioSource");

            this.audioLinkBehavior.SetProgramVariable<AudioSource>("audioSource", this.backupAudioSource);

            this.attachedAudioSource.enabled = false;
            this.audioInjectionFilter.enabled = false;

            this.audioLinkTakenOver = false;
        }

        private void EnableTakeover(bool enableFilter = false)
        {
            if (this.audioLinkTakenOver || !this.foundAudioLink) return;

            //this.audioLinkBehavior.publicVariables.

            //Il2CppSystem.Object _backupAudioSource;
            //bool success = this.audioLinkBehavior.publicVariables.TryGetVariableValue("audioSource", out _backupAudioSource);
            //this.backupAudioSource = _backupAudioSource.Cast<AudioSource>();
            //LoggerInstance.Msg(success ? "Successfully backed up original AudioSource" : "Failed to back up the original AudioSource");

            //Il2CppSystem.Type _type;
            //this.audioLinkBehavior.publicVariables.TryGetVariableType("audioSource", out _type);

            //_backupAudioSource.Unbox<AudioSource>();

            //LoggerInstance.Msg("type: " + _type.FormatTypeName());

            //this.audioLinkBehavior.publicVariables.TryGetVariableValue()

            //var varType = this.audioLinkBehavior.GetProgramVariableType("audioSource");
            this.backupAudioSource = this.audioLinkBehavior.GetProgramVariable("audioSource").Cast<AudioSource>();
            //this.backupAudioSource = previousAudioSource.Unbox<varType>();


            //success = this.audioLinkBehavior.publicVariables.TrySetVariableValue<AudioSource>("audioSource", this.attachedAudioSource);
            this.audioLinkBehavior.SetProgramVariable<AudioSource>("audioSource", this.attachedAudioSource);
            //LoggerInstance.Msg(success ? "Successfully added AudioSource to AudioLink" : "Could not set AudioLink's AudioSource");

            this.attachedAudioSource.enabled = true;
            this.audioInjectionFilter.enabled = enableFilter;

            this.audioLinkTakenOver = true;
        }
    }
}
