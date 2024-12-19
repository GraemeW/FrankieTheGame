# Assets: Game - Checks

Any game, and especially RPGs, require that a player's character interact with the world around them.  

<img src="../../../InfoTools/Documentation/Game/Checks/FrankieCheck.png" width="1080">

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

### Check with Dynamic Options

In some circumstances, the check options themselves may not be fixed/static, but may also vary as a function of the game state -- for these, one may employ the [CheckWithDynamicOptions](./CheckWithDynamicOptions.prefab) prefab.

This prefab makes use of a Dynamic Check Object that has a component implementing the [ICheckDynamic](../../Scripts/CheckInteractions/ICheckDynamic.cs) interface to return [ChoiceActionPairs](../../Scripts/Utils/Functional/ChoiceActionPair.cs) that are used to populate a menu of choices.  One such example is the [SubwayTrain](../WorldObjects/Subway/SubwayTrain/SubwayTrain.prefab), which has the [WorldSubwayRider](../../Scripts/World/Subway/WorldSubwayRider.cs) component to generate viable subway travel options:

<img src="../../../InfoTools/Documentation/Game/Checks/CheckWithDynamicOptionsSubwayTrain.png" width="1080">

### Check with Configuration

In order to allow for highly customized check behaviors, one may also employ the prefab [CheckWithConfiguration](./CheckWithConfiguration.prefab) with custom-scripted configurations.

Similarly to Dynamic Options above, this prefab uses a [CheckConfiguration](../../Scripts/CheckInteractions/Configurations/CheckConfiguration.cs) scriptable object to define [ChoiceActionPairs](../../Scripts/Utils/Functional/ChoiceActionPair.cs) that are used to populate a menu of choices.

Example configurations include:
* [AddToParty](./Configurations/AddToParty.asset) / [RemoveFromParty](./Configurations/RemoveFromParty.asset): with options populated by available characters / availability party members
* [AdjustLeader](./Configurations/AdjustLeader.asset): with options populated by available party members
* [BankOptions](./Configurations/BankOptions.asset): the basic script attached to the [ATM](../WorldObjects/PortaBank/ATM.prefab) prefab to enable deposit/withdrawal from Frankie's bank

Since the [CheckWithConfiguration](../../Scripts/CheckInteractions/CheckWithConfiguration.cs) script spawns a dialogue box with choice-action pairs, it's also possible to nest configurations within configurations -- such as with the [StandardPartyOptions](./Configurations/StandardPartyOptions.asset) configuration, which combines [AdjustLeader](./Configurations/AdjustLeader.asset), [AddToParty](./Configurations/AddToParty.asset) and [RemoveFromParty](./Configurations/RemoveFromParty.asset) configurations:

<img src="../../../InfoTools/Documentation/Game/Checks/CheckWithConfigurationStandardParty.png" width="550">
