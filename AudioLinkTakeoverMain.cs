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

        public AudioLinkTakeoverMain()
        {

        }

        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<AudioInjectionFilter>();
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
                    filter.enabled = true;

                    bool success = udonBehavior.publicVariables.TrySetVariableValue<AudioSource>("audioSource", emptyAudioSource);

                    LoggerInstance.Msg(success ? "set empty audio source" : "didnt work");
                }
            }
        }
    }
}
