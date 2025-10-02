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

For example, see [SpookyMusicOverride](./MusicOverrides/SpookyMusicOverride.prefab), which is a simple lightweight object that carries a reference to the override track (`audioClip`).  The [BackgroundMusicOverride](../../Scripts/Sound/BackgroundMusicOverride.cs) script has the public method `TriggerOverride(bool enable)` that can be called with UnityEvents (e.g. via [Check](../Checks/) interactions or [DialogueTriggers](../../Scripts/Speech/DialogueTrigger.cs)) to transition to the override audio.

Note that since a player may save/exit the game during an override audio, the [BackgroundMusicOverride](../../Scripts/Sound/BackgroundMusicOverride.cs) script includes state that is saveable/loadable via the [Save System](../../Scripts/Saving/).

## Sound Effects

### Standard Soundbox

All sound effects use the [SoundBox](./Soundbox.prefab) prefab or [standard variants of it](./StandardSoundboxes/).  

The [SoundBox](./Soundbox.prefab) carries a reference to an audio clip that can be played with public methods from the [SoundEffects](../../Scripts/Sound/SoundEffects.cs) script -- such as `PlayClip()` or `PlayClipAfterDestroy()`.  The latter method is used in instances where the soundbox may be destroyed or disabled before the sound effect finishes.  These methods are generally called by UnityEvents (e.g. via [Check](../Checks/) interactions or [DialogueTriggers](../../Scripts/Speech/DialogueTrigger.cs)).  

Examples of standard soundboxes include:
* [ZoneNodeSoundbox](./StandardSoundboxes/ZoneNodeSoundbox.prefab):  Attached to [ZoneNode objects](../WorldObjects/_ZoneNodes/) to play during transitions within or among zones (e.g. footsteps, door opening, etc.)
* [ShopSoundbox](./StandardSoundboxes/ShopSoundbox.prefab):  Attached to vendors, such as the vending machines found in [World objects](../WorldObjects/), to play a purchase confirmation sound
* [CharacterJoinPartySoundbox](./StandardSoundboxes/CharacterJoinPartySoundbox.prefab):  Used to play a congratulatory jingle on characters joining the party
* [DoorUnlockSoundbox](./StandardSoundboxes/DoorUnlockSoundbox.prefab) (+ [Lock](./StandardSoundboxes/DoorLockedSoundbox.prefab)):  Used to play an unlocking/locking sound when a door has been locked/unlocked
* [FallSoundbox](./StandardSoundboxes/FallSoundbox.prefab):  Used to play a whistle sound signifying that the player has fallen (e.g. down a hole)

### Specialized Soundboxes

Several key architectural game objects require more regular/tightly coupled interactions with sound effects, and thus use [SpecializedSoundboxes](./SpecializedSoundboxes/) built on child classes of the [SoundEffects](../../Scripts/Sound/SoundEffects.cs).

#### UIBox Soundbox

The [UIBoxSoundbox](./SpecializedSoundboxes/uiBoxSoundbox.prefab) helps to provide sound effects for [UI elements](../UI/) based on the [UIBox](../../Scripts/UI/UIBox/).

<img src="../../../InfoTools/Documentation/Game/Sound/UIBoxSoundbox.png" width="300">

Set:
* `uiBox`:  to the relevant [UIBox Implementation](../UI/)
  * *usually the soundbox will be childed to the UI box itself (thus attach the parent game object here)*
* `textScanAudioClip`:  to the sound to repeatedly play during text scan (i.e. as text prints to the screen)
  * `textScanLoopDelay`:  to the time delay in seconds between repeating the above sound during text scan
* `chooseAudioClip`:  to the sound to play during element selection in UI menus
* `enterClip`:  to the sound to play when the UIBox opens
* `exitClip`:  to the sound to play when thee UIBox closes

#### Fader Soundbox

The [FaderSoundBox](./SpecializedSoundboxes/FaderSoundbox.prefab) is childed to the [Fader](../Core/CoreDep/Fader.prefab).  [FaderSoundBox](./SpecializedSoundboxes/FaderSoundbox.prefab) is specifically used to manage the sounds for fading in/out of combat, and thus listens for relevant [TransitionTypes](../../Scripts/Zones/TransitionType.cs) passed by the `fadingIn` event from the [Fader](../Core/CoreDep/Fader.prefab).  In this regard, a different sound effect can be applied for different battle entry transitions (e.g. player surprises enemy vs. neutral vs. enemy surprises player).

*Note:  since we don't (yet) have sound effects generated for different battle transitions, the same neutral battle entry sound is currently applied for all transitions*

#### Combat Participant Soundbox

The [CombatParticipantSoundbox](./SpecializedSoundboxes/CombatParticipantSoundbox.prefab) is childed to any of the [CharacterObjects](../CharacterObjects/) that have a [CombatParticipant](../../Scripts/Combat/CombatParticipant/CombatParticipant.cs) attached to them.  Or, specifically, it is used for all (Playable) [Character](../CharacterObjects/PCs/Character.prefab) prefab variants as well as all [NPCCombatReady](../CharacterObjects/NPCs/NPCCombatReady.prefab) prefab variants.  

[CombatParticipantSoundbox](./SpecializedSoundboxes/CombatParticipantSoundbox.prefab)  is used for virtually all sound effects that trigger during combat.  

<img src="../../../InfoTools/Documentation/Game/Sound/CombatParticipantSoundbox.png" width="300">

It listens for the `stateAltered` from its parent CombatParticipant to identify any events that require an associated sound effect, such as:
* `StateAlteredType.DecreaseHP`:  to play the `decreaseHPAudioClip`
* `StateAlteredType.IncreaseHP`:  to play the `increaseHPAudioClip`
* `StateAlteredType.Dead`:  to play the `deadAudioClip`

, and so on…

Note that with the sound effect living directly on the character (instead of associated with the combat or BattleController), these sound effects can play whether in combat or in the world.  In other words, if a character receives damage or dies in the world, the same familiar sound effects will play.
