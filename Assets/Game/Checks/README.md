# Assets: Game - Checks

Any game, and especially RPGs, require that a player's character interact with the world around them.  

'Check' prefabs serve to enable this functionality in Frankie.  These are game objects that can be attached/childed to any other game objects in the world to enable sed interaction.  

All 'checks' thus include:
* A) a trigger-based 2D collider
* B) a 'check' component derived from the [CheckBase](../../Scripts/CheckInteractions/CheckBase.cs) abstract class

, with additional levels of varying complexity to allow for specific interactions -- e.g. with messages, with further user input, toggling as a function of predicates, with more numerous options/outcomes, etc.

## Standard Checks

The standard check configurables are shown below:

<img src="../../../InfoTools/Documentation/Game/Checks/StandardChecks.png" width="450">

Notably, standard checks are brokebn into three different check types.

* **Simple** - with prefab [Check](./Check.prefab)
  * Allowing for triggering arbitrary Unity Event(s) w/ player interaction
    * as linked on the component, shown in the list under `Check Interaction (PlayerStateMachine)`
    * note: this UnityEvent passes the player state machine as a parameter to allow for more complex interactions
* **Message** - with prefab [CheckWithMessage](./CheckWithMessage.prefab)
  * Allowing for the same, while also prompting a UI dialogue/text box to appear with a defined message
  * For this option, `{0}` can be used to use the party leader's name
  * The check interaction will, by default, trigger after the UI dialogue/text box is closed
    * the `Check At Start of Interaction` parameter can be set true to instead trigger the Unity Event(s) before the UI text/dialogue box is prompted
* **ChoiceConfirmation** - with prefab [CheckWithConfirmationComponent](./CheckWithConfirmationComponent.prefab)
  * Allowing for the same, while instead prompting a UI dialogue/text box with a yes/no confirmation option
  * For this option, independent message text & Unity Event(s) can be defined for either:
    * accepting - via `Check Interaction (PlayerStateMachine)` list
    * rejecting - via `Reject Interaction (PlayerStateMachine)` list

Example checks can be found in:
* [FrankieHomeDesk](../WorldObjects/SavePoints/FrankieHomeDesk.prefab): Used to trigger a [WorldSaver](../../Scripts/World/WorldSaver.cs) Save() call
* [WaterCooler](../WorldObjects/Office/WaterCooler_0.prefab): Used to trigger a [WorldPointAdjuster](../../Scripts/World/WorldPointAdjuster.cs) to heal the party & restore AP
* [InfoSign](../WorldObjects/Generic/Signs/InfoSign.prefab): Used to prompt a UI message with th (arbitrary) sign content
* [CoinMachine](../WorldObjects/VendingMachines/CoinMachine.prefab): Used to trigger a [WorldCashGiverTaker](../../Scripts/World/WorldCashGiverTaker.cs) to give add cash to the player's wallet
* and so on…

## Complex Checks

Beyond the straightforward checks noted above, there are a number of relatively common checks with higher levels of complexity that need to be supported for a standard RPG.  For these more common checks, generic implementations and prefabs (also derived from the [CheckBase](../../Scripts/CheckInteractions/CheckBase.cs)  abstract class) are detailed below.

### Check With Predicate Evaluation

The prefab [CheckWithPredicateEvaluation](./CheckWithPredicateEvaluation.prefab) can be used to evaluate any arbitrary [predicate](../../Scripts/Predicates/Predicate.cs) condition, and then prompt independent messages + Unity Events on success/failure.  This can be useful, for example, for only prompting events/behaviour when the player has progressed past a certain quest, or a specific character is above a certain level, or the player has a certain amount of cash, or a specific key item is in a party member's knapsack, etc.

The check configurables are shown below:

<img src="../../../InfoTools/Documentation/Game/Checks/CheckWithPredicates.png" width="700">

, with:
* Predicate field used to check the specific game state / user state / condition
* Condition Met: message + interaction Unity Event(s) for predicate = true
* Condition Failed: message + interaction Unity Event(s) for predicate = false

Example checks can be found in:
* [VendingMachineWithPredicate](../WorldObjects/VendingMachines/VendingMachineWithPredicate.prefab)

### Check to Unlock Enable

Extending on the above, it is very common to gate access to specific areas or interaction behind predicate evaluations.  One way to do this is to place game objects in a scene, disable them by default, and then enable them through an interaction event.

The prefab [CheckToUnlockEnable](./CheckToUnlockEnable.prefab) serves as a standard check to serve this purpose.  The check configuration is shown below:

<img src="../../../InfoTools/Documentation/Game/Checks/CheckWithToggleEnable.png" width="650">

, with:
* Predicate field used to check the specific game state / user state / condition
* Condition Met: `Message On Toggle` and `Check Interation (PlayerStateMachine)` for predicate = true
* Condition Failed:  `Message On Condition Not Met` and `Check Interaction On Condition Not Met (PlayerStateMachine)` for predicate = false
* Parent Transform For Toggle: reference transform for toggling->active on all children game objects
  * *by default this is the game object 'UnlockParent' childed to the  [CheckToUnlockEnable](./CheckToUnlockEnable.prefab) prefab*

For this check prefab, standard use is to a) child the prefab on an object in the world (e.g. attach to a door or ZoneNode), b) place all inactive game objects under the `UnlockParent` game object.

Example checks can be found in:
* [OfficeExterior](../../Scenes/OfficeExterior.unity) scene, under the `UnlockToOffice` ZoneNode
* [OfficeInterior](../../Scenes/OfficeInterior.unity) scene, under the `UnlockWithKey` ZoneNode
  ** which is used to unlock the fireman's pole after retrieving the necessary key, as below:*

<img src="../../../InfoTools/Documentation/Game/Checks/CheckWithToggleEnable_OfficeKey.png" width="750">

### Check with Configuration

### Check with Dynamic Options


