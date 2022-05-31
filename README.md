# Local AudioLink Takeover

A mod for VRChat that enables you to feed audio from any output device or input device (like a microphone) into the AudioLink running in a world or to disable it completely.
You can use this to have AudioLink visualize other music than what's playing in a world.

Use cases might be:

- listening to Spotify or twitch while in a club
- having the lights in a club be synchronized with the music for a stream (instead of the lights being multiple seconds behind)

## Installation

1. Have MelonLoader 0.5 or later installed.

2. Install UiExpansionKit, for example by using [VRCMelonAssistant](https://github.com/knah/VRCMelonAssistant).

3. Go to the [releases](https://github.com/jangxx/VRCLocalAudioLinkTakeover/releases) section and download the latest release.

4. Put LocalAudioLinkTakeover.dll into the _Mods/_ folder within the VRChat directory.

## Usage

The mod adds an "AudioLink Takeover" button into the quick menu.
Clicking on it will open an interface with the following buttons:

- "Release Takeover": Restores the original AudioLink source (Basically disables the mod)
- "Disable AudioLink": Overwrites the AudioLink input with an AudioSource that only plays silence, effectively turning it off
- Loads of buttons labeled with output and input names: Clicking on these will send the audio from that specific device into AudioLink and visualize it.

## Settings

The mod puts a few settings into the Mod settings UI:

- "Minimum Boost": The minimum amount of boost that is applied to the audio signal
- "Maximum Boost": The maximum amount of boost that can be applied to the audio signal. If the signal ever clips, the boost will be reduced so that the current sample is just at the clip level. It will then slowly increase over time again until it gets docked again.
- "Boost increase per second": Amount of boost increase per second (up to the maximum).