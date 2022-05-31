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
        private GameObject audioSourceContainer = null;
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

            MelonPreferences.CreateCategory("LocalAudioLinkTakeover", "Audio Link Takeover");
            MelonPreferences.CreateEntry<float>("LocalAudioLinkTakeover", "MinBoost", this.outputWaveProvider.MinBoost, "Minimum Boost");
            MelonPreferences.CreateEntry<float>("LocalAudioLinkTakeover", "MaxBoost", this.outputWaveProvider.MaxBoost, "Maximum Boost");
            MelonPreferences.CreateEntry<float>("LocalAudioLinkTakeover", "BoostSpeed", this.outputWaveProvider.BoostIncreaseSpeed, "Boost increase per second");

            this.quickMenuSettings.MenuInteractionEvent += OnMenuInteraction;
            this.quickMenuSettings.Init();

            OnPreferencesSaved();
        }

        public override void OnPreferencesSaved()
        {
            this.outputWaveProvider.MinBoost = MelonPreferences.GetEntryValue<float>("LocalAudioLinkTakeover", "MinBoost");
            this.outputWaveProvider.MaxBoost = MelonPreferences.GetEntryValue<float>("LocalAudioLinkTakeover", "MaxBoost");
            this.outputWaveProvider.BoostIncreaseSpeed = MelonPreferences.GetEntryValue<float>("LocalAudioLinkTakeover", "BoostSpeed");
        }

        public void OnMenuInteraction(object sender, EventArgs args)
        {
            var menuEventArgs = (MenuInteractionEventArgs)args;

            switch (menuEventArgs.Type)
            {
                case MenuInteractionEventArgs.MenuInteractionType.DISABLE_AUDIOLINK:
                    DisableCapture();
                    EnableTakeover(true); // enable filter but it will only feed in zeroes
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

            int channels = 2;

            if (input) // if this is an input device use the normal Wasapi capture
            {
                // create an audio client to query the format this input device wants
                var tempAudioClient = CSCore.CoreAudioAPI.AudioClient.FromMMDevice(device);
                var bestFormat = tempAudioClient.MixFormat;

                channels = bestFormat.Channels;

                this.cs_captureDevice = new CSCore.SoundIn.WasapiCapture(true, CSCore.CoreAudioAPI.AudioClientShareMode.Shared, 15, new WaveFormat(48000, 32, channels, AudioEncoding.IeeeFloat));
            }
            else // otherwise use the loopback capture
            {
                this.cs_captureDevice = new CSCore.SoundIn.WasapiLoopbackCapture(5, new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat));
            }

            this.cs_captureDevice.Device = device;
            this.cs_captureDevice.Initialize();

            this.cs_recorder = new CSCore.Streams.SoundInSource(this.cs_captureDevice, 2048 * channels * 32/8);

            this.cs_captureDevice.Start();
            this.outputWaveProvider.SetInput(this.cs_recorder);
            this.outputWaveProvider.Format = this.cs_captureDevice.WaveFormat;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            this.foundAudioLink = false;
            this.audioLinkTakenOver = false;
            this.audioSourceContainer = null;

            var udonBehaviors = UnityEngine.Object.FindObjectsOfType<VRC.Udon.UdonBehaviour>();

            foreach (var udonBehavior in udonBehaviors)
            {
                if (udonBehavior.name == "AudioLink")
                {
                    LoggerInstance.Msg("Found AudioLink behavior");
                    this.foundAudioLink = true;

                    this.audioSourceContainer = new GameObject("Audio Injector");

                    this.audioLinkBehavior = udonBehavior;

                    this.attachedAudioSource = this.audioSourceContainer.AddComponent<AudioSource>();
                    this.attachedAudioSource.volume = 0.001f;
                    this.attachedAudioSource.enabled = false;

                    this.audioInjectionFilter = this.audioSourceContainer.AddComponent<AudioInjectionFilter>();
                    this.audioInjectionFilter.enabled = false;
                    this.audioInjectionFilter.SetInputProvider(this.outputWaveProvider);

                    // add as a child of the audiolink object
                    this.audioSourceContainer.transform.parent = udonBehavior.gameObject.transform;

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
            this.audioSourceContainer = null;

            this.foundAudioLink = false;
            this.audioLinkTakenOver = false;
        }

        private void ReleaseAudioLinkTakeover()
        {
            if (!this.audioLinkTakenOver || !this.foundAudioLink) return;

            if (this.backupAudioSource == null) return;

            try
            {
                this.audioLinkBehavior.SetProgramVariable<AudioSource>("audioSource", this.backupAudioSource);

                this.attachedAudioSource.enabled = false;
                this.audioInjectionFilter.enabled = false;

                this.audioLinkTakenOver = false;
            } catch (Exception ex)
            {
                LoggerInstance.Error("Error while releasing AudioLink: " + ex.ToString());
            }
        }

        private void EnableTakeover(bool enableFilter = false)
        {
            if (this.audioLinkTakenOver || !this.foundAudioLink) return;

            try
            {
                this.backupAudioSource = this.audioLinkBehavior.GetProgramVariable("audioSource").Cast<AudioSource>();
            } catch (Exception ex) { }

            try
            {
                this.audioLinkBehavior.SetProgramVariable<AudioSource>("audioSource", this.attachedAudioSource);

                this.attachedAudioSource.enabled = true;
                this.audioInjectionFilter.enabled = enableFilter;

                this.audioLinkTakenOver = true;
            } catch (Exception ex) { 
                LoggerInstance.Error("Error while taking over AudioLink: " + ex.ToString());
            }
        }
    }
}
