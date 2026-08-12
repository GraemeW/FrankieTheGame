# Assets:  Game - Controllers

Controllers are used to translate player input into relevant on-screen actions in Frankie.  They each interface with Unity's New Input System via their corresponding [PlayerInput.inputactions](../../UnityConfigurables/InputProfiles/PlayerInput.inputactions) file and associated [PlayerInput.cs](../../UnityConfigurables/InputProfiles/PlayerInput.cs) script by listening to `.performed` events.

Since there are several interaction mechanisms that vary as a function of [PlayerState](../../Scripts/Core/PlayerStateMachine/PlayerStates/IPlayerState.cs), there are, accordingly, several types of controllers:
* [PlayerController](../Core/README.md#player-prefab-singleton):  existing on the [Player](../Core/Player.prefab) prefab, for handling in-world input
  * *via [PlayerController Script](../../Scripts/Control/Controllers/PlayerController.cs)*
* [BattleController](./Battle%20Controller.prefab):  for handling input while the player is in combat
  * *via [BattleController Script](../../Scripts/Control/Controllers/BattleController.cs)*
* [DialogueController](./DialogueController.prefab):  for handling input while the player is in dialogue
  * *via [DialogueController Script](../../Scripts/Control/Controllers/DialogueController.cs)*

There are also two lightweight/mini-controllers used for specific scenes:
* [SplashMenuController](./SplashMenuController.prefab):  for input during splash screens (i.e. [SplashScreen](../../Scenes/SplashScreen.unity))
* [MainMenuController](./MainMenuController.prefab):  for input in the game start / load game menus (i.e. [StartScreen](../../Scenes/StartScreen.unity))

The configuration of each controller is not covered in detail here as the parameters are quite straightforward.  For more information on the implementation and functionality of each controller, see [Controllers Scripts](../../Scripts/Control/Controllers/) and [Player Scripts](../../Scripts/Control/Player/).

## Life Cycle of a Controller

### Controller Instantiation via PlayerStateMachine

The [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs) on the [Player](../Core/Player.prefab) has public methods to transition across different [PlayerStates](../../Scripts/Core/PlayerStateMachine/PlayerStates/IPlayerState.cs).  

Starting from the [WorldState](../../Scripts/Core/PlayerStateMachine/PlayerStates/WorldState.cs), for example, the [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs) may receive a cue to:
* `EnterCombat()` -- e.g. from an [NPCStateHandler](../../Scripts/Control/NPC/NPCStateHandler.cs)'s `InitiateCombat()`

or

* `EnterDialogue()` -- e.g. from an [NPCStateHandler](../../Scripts/Control/NPC/NPCStateHandler.cs)'s `InitiateDialogue()`

As part of this transition, the [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs) instantiates and sets up the corresponding [BattleController](./Battle%20Controller.prefab) or [DialogueController](./DialogueController.prefab).  The [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs) also announces the state change via the `Action<PlayerStateType> playerStateChanged` event, which temporarily pauses input from the [PlayerController](../Core/README.md#player-prefab-singleton) that may cause issues.  

### Controller Behaviour

Once a new controller is instantiated, it will monitor for player input and update game state accordingly.  As noted above, see [Controllers Scripts](../../Scripts/Control/Controllers/) for more detail on implementation.

### Input Receivers and Subscription to Controllers

All controllers derive from the [BaseController](../../../Packages/com.lowdefmustard.control/Runtime/BaseController.cs) abstract class, and thus support the `AddInputReceiver(IInputReceiver inputReceiver, Action disableCallbacks)` method.

This allows one to add any number of [IInputReceivers](../../../Packages/com.lowdefmustard.control/Runtime/InputReceiver/IInputReceiver.cs) onto the BaseController to temporarily take control of the user input.  When an IInputReceiver is destroyed, the Controller will automatically pass control to the next top-most receiver in the controller's stack (as a LIFO-type implementation).

For example, one may spawn a [DialogueOptionBox](../UI/Speech/DialogueOptionBox.prefab), which implements the [UIBox](../../../Packages/com.lowdefmustard.uibox/Runtime/UIBox.cs) and assign it to the PlayerController via `AddInputReceiver(dialogueOptionBox, null)`.  This temporarily makes a window that appears to the user with some options.  The window may in itself spawn a second DialogueOptionBox, which can also be added to the PlayerController.  Once the second window is closed/destroyed, control passes back to the first window, and once the first window is destroyed, control then passes back to the base PlayerInputController (e.g. if in world, the player resumes control to move their character).  Practically, these fine details are all managed via access methods through the [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs)

### Controller Destruction

#### BattleController

As part of the `SetupBattleController()`, the [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs) subscribes to [BattleState](../../Scripts/Combat/DataStructuresInterfaces/BattleState.cs) events.  

When the [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs) hears `BattleState.Complete`, it will transition out of the battle:
* initially from [CombatState](../../Scripts/Core/PlayerStateMachine/PlayerStates/CombatState.cs) to [TransitionState](../../Scripts/Core/PlayerStateMachine/PlayerStates/TransitionState.cs)
  * *this allows us to paint a battle transition screen via [Fader](../Core/CoreDep/Fader.prefab)*
* then from [TransitionState](../../Scripts/Core/PlayerStateMachine/PlayerStates/TransitionState.cs) to [WorldState](../../Scripts/Core/PlayerStateMachine/PlayerStates/WorldState.cs)

, and trigger `QueueExitCombat()` in kind, which destroys the current BattleController.

#### DialogueController

When dialogue has completed or the player has exited out of the dialogue box, the DialogueController will call `EndConversation()`.  This triggers the [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs) to transition from [DialogueState](../../Scripts/Core/PlayerStateMachine/PlayerStates/DialogueState.cs) to [WorldState](../../Scripts/Core/PlayerStateMachine/PlayerStates/WorldState.cs) and destroys the DialogueController.

#### Polling to Kill Rogue Controllers

In addition to standard destruction methods, controllers also periodically poll for the scenario where they're initialized, but no longer have any valid input receivers.  This is handled in the [BaseController](../../../Packages/com.lowdefmustard.control/Runtime/BaseController.cs) abstract class, via the `PollForReceivers(float deltaTime)` method.

If a rogue controller is identified, it will:
* attempt to push the player back into the World State via the [PlayerStateMachine's](../../Scripts/Core/PlayerStateMachine.cs) `EnterWorld()` method
* make note of its rogue existence as a debug log warning
* destroy itself
