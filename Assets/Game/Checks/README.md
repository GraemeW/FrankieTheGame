# Assets: Game - Checks

Any game, and especially RPGs, require that a player's character interact with the world around them.  

'Check' prefabs serve to enable this functionality in Frankie.  These are game objects that can be attached/childed to any other game objects in the world to enable sed interaction.  

All 'checks' thus include:
* A) a trigger-based 2D collider
* B) a 'check' component derived from the [CheckBase](../../Scripts/CheckInteractions/CheckBase.cs) abstract class

, with additional levels of varying complexity to allow for specific interactions -- e.g. with messages, with further user input, toggling as a function of predicates, with more numerous options/outcomes, etc.

## Standard Checks

A standard check can be:
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

These options and the associated configurables are shown below:

<img src="../../../InfoTools/Documentation/Game/Checks/StandardChecks.png" width="450">

## Check With Predicate Evaluation

## Check to Unlock Enable

## Check with Configuration

## Check with Dynamic Options


