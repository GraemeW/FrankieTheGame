# Assets:  Game - Sound

## Background Music

All background music is played by the [BackgroundMusic](./BackgroundMusic.prefab) prefab, which is childed to the [PersistentObjects](../Core/README.md#persistent-objects-singleton) singleton.  [BackgroundMusic](./BackgroundMusic.prefab) employs a [Unity Audio Mixer](https://docs.unity3d.com/6000.1/Documentation/Manual/AudioMixer.html) in order to master all tracks as well as handle track transitions.

### Configuration

<img src="../../../InfoTools/Documentation/Game/Sound/BackgroundMusic.png" width="300">

Set:
* `volume`:  0.0f to 1.0f to set volume to min to max respectively
* `musicFadeDuration`:  duration in seconds for fading between tracks
* `audioMixer`:  typically fixed, set to [FrankieMusicMixer](./FrankieMusicMixer.mixer)
* Standard Fixed Audio
  * `levelUpAudio`:  track to play at end of combat when characters level up, typically fixed

### Handling Zone Music

As described in [Zones->Configuration](../OnLoadAssets/Zones/README.md#configure-the-zone), each zone (scene) has a specific `zoneAudio` (AKA world background music).  This music is played once the player enters the zone.

In order to load the correct zone music, [BackgroundMusic](./BackgroundMusic.prefab) listens for `zoneUpdated` events from the [SceneLoader](../../Scripts/Zones/SceneLoader.cs), which carries a reference to the new zone.  That reference is used to pull the relevant audio clip to transition the audio accordingly.

### Handling Combat Music

As described in [CharacterObjects->Combat Setup](../CharacterObjects/README.md#combat-setup), each enemy has a specific `combatAudio` (AKA battle music) to play during combat.  

In order to load the correct battle music, [BackgroundMusic](./BackgroundMusic.prefab) listens for `playerStateChanged` events from the [PlayerStateMachine](../../Scripts/Control/Player/PlayerStateMachine.cs) that lives on the [Player](../Core/README.md#player-prefab-singleton) singleton.  [BackgroundMusic](./BackgroundMusic.prefab) will then find the [BattleController](../Controllers/README.md#battlecontroller) and identify the correct music to play from the enemies currently in combat.

While setting up the combat music, [BackgroundMusic](./BackgroundMusic.prefab) subscribes to the [BattleController](../Controllers/README.md#battlecontroller)'s `battleStateChanged` event in order to listen for specific battle-related prompts for music updates (e.g. characters leveling up).

### Other Music Overrides

In some instances, the background music needs to be overridden while within a given scene (e.g. for dramatic effect).  In these cases, a [MusicOverride](./MusicOverrides/) game object can be used.  

For example, see [SpookyMusicOverride](./MusicOverrides/SpookyMusicOverride.prefab), which is a simple lightweight object that carries a reference to the override track (`audioClip`).  The [BackgroundMusicOverride](../../Scripts/Sound/BackgroundMusicOverride.cs) script has the public method `TriggerOverride(bool enable)` that can be called via UnityEvents (e.g. via [Check](../Checks/) interactions or [DialogueTriggers](../../Scripts/Speech/DialogueTrigger.cs)) to transition to the override audio.

Note that since a player may save/exit the game during an override audio, the [BackgroundMusicOverride](../../Scripts/Sound/BackgroundMusicOverride.cs) script includes state that is saveable/loadable via the [Save System](../../Scripts/Saving/).

## Sound Effects

### Standard Soundbox

### Specialized Soundboxes

#### UIBox Soundbox

#### Fader Soundbox

#### Combat Participant Soundbox

