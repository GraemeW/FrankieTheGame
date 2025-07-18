# Assets:  Game - Controllers

Controllers are used to translate player input into relevant on-screen actions in Frankie.  They each interface with Unity's New Input System via their corresponding [PlayerInput.inputactions](../../Scripts/Control/Input/InputProfiles/PlayerInput.inputactions) file and associated [PlayerInput.cs](../../Scripts/Control/Input/InputProfiles/PlayerInput.cs) script by listening to `.performed` events.

Since there are several interaction mechanisms that vary as a function of [PlayerState](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/IPlayerState.cs), there are, accordingly, several types of controllers:
* [PlayerController](../Core/README.md#player-prefab-singleton):  existing on the [Player](../Core/Player.prefab) prefab, for handling in-world input
  * *via [PlayerController Script](../../Scripts/Control/Player/PlayerController.cs)*
* [BattleController](./Battle%20Controller.prefab):  for handling input while the player is in combat
  * *via [BattleController Script](../../Scripts/Control/Controllers/BattleController.cs)*
* [DialogueController](./DialogueController.prefab):  for handling input while the player is in dialogue
  * *via [DialogueController Script](../../Scripts/Control/Controllers/DialogueController.cs)*

There are also two lightweight/mini-controllers used for specific scenes:
* [SplashMenuController](./SplashMenuController.prefab):  for input during splash screens (i.e. [SplashScreen](../../Scenes/SplashScreen.unity))
* [StartMenuController](./StartMenuController.prefab):  for input in the game start / load game menus (i.e. [StartScreen](../../Scenes/StartScreen.unity))

The configuration of each controller is not covered in detail here as the parameters are quite straightforward.  For more information on the implementation and functionality of each controller, see [Controllers Scripts](../../Scripts/Control/Controllers/) and [Player Scripts](../../Scripts/Control/Player/).

## Life Cycle of a Controller

### Controller Instantiation via PlayerStateMachine

The [PlayerStateMachine](../../Scripts/Control/Player/PlayerStateMachine.cs) on the [Player](../Core/Player.prefab) has public methods to transition across different [PlayerStates](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/IPlayerState.cs).  

Starting from the [WorldState](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/WorldState.cs), for example, the [PlayerStateMachine](../../Scripts/Control/Player/PlayerStateMachine.cs) may receive a cue to:
* `EnterCombat()` -- e.g. from an [NPCStateHandler](../../Scripts/Control/NPC/NPCStateHandler.cs)'s `InitiateCombat()`

or

* `EnterDialogue()` -- e.g. from an [NPCStateHandler](../../Scripts/Control/NPC/NPCStateHandler.cs)'s `InitiateDialogue()`

As part of this transition, the [PlayerStateMachine](../../Scripts/Control/Player/PlayerStateMachine.cs) instantiates and sets up the corresponding [BattleController](./Battle%20Controller.prefab) or [DialogueController](./DialogueController.prefab).  The [PlayerStateMachine](../../Scripts/Control/Player/PlayerStateMachine.cs) also announces the state change via the `Action<PlayerStateType> playerStateChanged` event, which temporarily pauses input from the [PlayerController](../Core/README.md#player-prefab-singleton) that may cause issues.  

### Controller Behaviour

Once a new controller is instantiated, it will monitor for player input and update game state accordingly.  As noted above, see [Controllers Scripts](../../Scripts/Control/Controllers/) and [Player Scripts](../../Scripts/Control/Player/) for more detail on implementation.

### Controller Destruction

#### BattleController

As part of the `SetupBattleController()`, the [PlayerStateMachine](../../Scripts/Control/Player/PlayerStateMachine.cs) subscribes to [BattleState](../../Scripts/Combat/DataStructuresInterfaces/BattleState.cs) events.  

When the [PlayerStateMachine](../../Scripts/Control/Player/PlayerStateMachine.cs) hears `BattleState.Complete`, it will transition out of the battle:
* initially from [CombatState](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/CombatState.cs) to [TransitionState](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/TransitionState.cs)
  * *this allows us to paint a battle transition screen via [Fader](../Core/CoreDep/Fader.prefab)*
* then from [TransitionState](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/TransitionState.cs) to [WorldState](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/WorldState.cs)

, and trigger `QueueExitCombat()` in kind, which destroys the current BattleController.

#### DialogueController

When dialogue has completed or the player has exited out of the dialogue box, the DialogueController will call `EndConversation()`.  This triggers the [PlayerStateMachine](../../Scripts/Control/Player/PlayerStateMachine.cs) to transition from [DialogueState](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/DialogueState.cs) to [WorldState](../../Scripts/Control/Player/PlayerStateMachine/PlayerStates/WorldState.cs).

The DialogueController will subsequently kill itself via its `KillControllerForNoReceivers()` method.
