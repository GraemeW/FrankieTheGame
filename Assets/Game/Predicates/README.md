# Assets:  Game - Predicates

Predicates are a flexible system to answer the "if {this} then {that}" game logic throughout Frankie.  They are lightweight scriptable objects (SOs) that are consumed by [IPredicateEvaluators](../../Scripts/Predicates/IPredicateEvaluator.cs) to return `true`, `false` or `don't know` to:

```bool? Evaluate(Predicate predicate);```

## Predicate Integration

Predicates are integrated to glean information from most core systems in Frankie, such as:
* Dialogue - via [AIConversant Predicates](./AIConversant/)
  * e.g. who is talking?, how many times have they talked?, etc.
  * for use with `IPredicateEvaluator` implemented by the [AIConversant Script](../../Scripts/Speech/AIConversant.cs), living on [Character Objects](../CharacterObjects/)
* Stats/Character - via [BaseStats Predicates](./BaseStats/)
  * e.g. is this a specific character?, is their beauty stat a certain value?, etc.
  * for use with `IPredicateEvaluator` implemented by the [BaseStats Script](../../Scripts/Stats/BaseStats.cs), living on [Character Objects](../CharacterObjects/)
* Combat Status - via [CombatParticipant Predicates](./CombatParticipant/)
  * e.g. is this character dead?  do they have a specific status effect?, etc.
  * for use with `IPredicateEvaluator` implemented by the [CombatParticipant Script](../../Scripts/Combat/CombatParticipant/CombatParticipant.cs), living on [Character Objects](../CharacterObjects/)
* Inventory - via [Knapsack Predicates](./Knapsack/)
  * e.g. is a specific item in the player's inventory?, does the player have any consumable items?, etc.
  * for use with `IPredicateEvaluator` implemented by the [Knapsack Script](../../Scripts/Inventory/Knapsack.cs), living on [Playable Character Objects](../CharacterObjects/PCs/)
* Party Status - via [Party](./Party/) predicates
  * e.g. is a specific character in the party?, is the leader of the party Frankie?, etc.
  * for use with `IPredicateEvaluator` implemented by the [Party Script](../../Scripts/Stats/Party/Party.cs), living on the [Player Object](../Core/Player.prefab)
* Quests - via [QuestList](./QuestList/) predicates
  * e.g. is a specific quest completed?, how about a specific objective within a quest?, etc.
  * for use with `IPredicateEvaluator` implemented by the [QuestList Script](../../Scripts/Quests/QuestList.cs), living on the [Player Object](../Core/Player.prefab)

Expanding on existing integrations is relatively trivial by defining new concrete child SOs based on the relevant abstract sub-predicate SO.  Creating completely new integrations (e.g. beyond those noted above) can be done via creation of a new abstract sub-predicate SO and a new `IPredicateEvaluator` to evaluate sed sub-predicates.  See [Predicate Scripts](../../Scripts/Predicates/) for more detail.

### Predicate Creation:  Quick-Start Guide

#### Make the Predicate

1. Navigate to the relevant sub-folder from within this [Predicates](./) directory
2. Right click and select `Create` -> `Predicates` -> …`desired sub-predicate`…

<img src="../../../InfoTools/Documentation/Game/Predicates/NewPredicateMenu.png" width="500">

#### Configure:  AI Conversant

To configure the `ConversantCheckCount` SO, set:
* `Check Count`:  to the number of times the conversant has spoken to the player
* `IsEqual`:  true if the criteria should be met exactly
* , otherwise `IsGreater`:  
  * true if # interactions > `Check Count`
  * false if # interactions < `Check Count`

For example, see below to check if the conversant has spoken once already:

<img src="../../../InfoTools/Documentation/Game/Predicates/ConfigureAIConversant.png" width="300">

#### Configure:  BaseStats - IsCharacter

Set:
* `Character`: to the desired [CharacterProperties](../OnLoadAssets/CharacterProperties/)

For example, see below to check if the character of interest is [PhilToo](../OnLoadAssets/CharacterProperties/PhilToo.asset)

<img src="../../../InfoTools/Documentation/Game/Predicates/ConfigureBaseStatsIsCharacter.png" width="300">

*Note:  These single character check predicates are commonly used on [EquipableItems](../OnLoadAssets/Inventory/EquipableItems/).  For checks on presence of a character in party, see [Configure: Party](#configure--party)*

#### Configure:  BaseStats - CharacterStatExceedsValue

Set:
* `Character`:  to the desired [CharacterProperties](../OnLoadAssets/CharacterProperties/)
* `Stat`:  to the desired [Stat](../../Scripts/Stats/Stat.cs)
* `Value`:  to the value to check the character's stat against
  * Note:  returns true for >= `Value`

For example, see below to check if Frankie has a Beauty stat greater than or equal to 20:

<img src="../../../InfoTools/Documentation/Game/Predicates/ConfigureBaseStatsHitSpec.png" width="300">

#### Configure:  CombatParticipant

To configure the `IsCharacterDead` SO, set:
* `Characters`:  to the desired [CharacterProperties](../OnLoadAssets/CharacterProperties/)

For example, see below to check if [Horse](../OnLoadAssets/CharacterProperties/Horse.asset) is dead:

<img src="../../../InfoTools/Documentation/Game/Predicates/ConfigureCombatParticipant.png" width="300">

*Note: the `IsAnyoneAlive` and `IsAnyoneDead` predicates do not need to be configured, as they operate without input variables*

#### Configure:  Knapsack - Has Item

Set:
* `Inventory Items`:  to the desired [Items](../OnLoadAssets/Inventory/)

For example, see below to check if the [KeyCardToOffice](../OnLoadAssets/Inventory/KeyItems/KeyCardToOffice.asset) is in any of the party character's knapsacks:

<img src="../../../InfoTools/Documentation/Game/Predicates/ConfigureKnapsack.png" width="300">

*Note: these predicates operate to return true if **any** of the items are present in any knapsack in the party*

#### Configure:  Party

To configure either of the `ContainsAnyCharacter` or `IsLeader` SOs, set:
* `Characters To Match`: to the desired [CharacterProperties](../OnLoadAssets/CharacterProperties/)

For example, see below to check if [Frankie](../OnLoadAssets/CharacterProperties/Frankie.asset) is in the party:

<img src="../../../InfoTools/Documentation/Game/Predicates/ConfigureParty.png" width="300">

*Note: these predicates operate to return true if **any** of the party members satisfy the criteria*

#### Configure QuestList

To configure either of the `QuestObjectiveCompleted` or `QuestCompleted` SOs, set:
* `Quest`: to the desired [Quest](../OnLoadAssets/Quests/)
* `Objective`: to the desired `QuestObjective` (childed to the Quest)
  * Note:  leave this parameter blank for `QuestCompleted`

For example, see below to check if the objective `HorseReceivedTheDocuments` has been completed for the quest [OfficeInteriorHorse](../OnLoadAssets/Quests/OfficeInteriorHorse.asset):

<img src="../../../InfoTools/Documentation/Game/Predicates/ConfigureQuestList.png" width="300">

## Predicate Evaluation

Predicate evaluation is initiated by a [Condition](../../Scripts/Predicates/Condition.cs), which follows the [conjunctive normal form](https://en.wikipedia.org/wiki/Conjunctive_normal_form) to logically stitch together a series of predicates.  

The [Condition](../../Scripts/Predicates/Condition.cs) makes use of `IPredicateEvaluator`s, which (in practice) are pulled off of the [Player](../Core/Player.prefab) game object and its children, as well as any interacting object (e.g. an NPC talking to the player).  Given this description, conditions for predicate evaluation can be easily implemented on any component that can get access to the active [Player](../Core/Player.prefab) (or any of its components -- such as the [PlayerStateMachine](../Core/)).

### Use-Cases to Evaluate Predicates

The most common instances to evaluate predicates (i.e. scripts that have a [Condition](../../Scripts/Predicates/Condition.cs) for execution) include:
* [Dialogue Nodes](../../Scripts/Speech/DialogueNode.cs), living on the Dialogue SOs in [Speech](../Speech/)
  * , to conditionally enable/disable various branches of a character's dialogue
* [Zone Nodes](../../Scripts/Zones/ZoneNode.cs), living on the Zone SOs in [OnLoadAssets/Zones](../OnLoadAssets/Zones/)
  * , to conditionally enable/disable access to zones (scenes) or specific areas within a zone
* [Equipable Item](../OnLoadAssets/Inventory/EquipableItems/) SOs
  * , to add stat or level requirements on equipment
* [Check Interactions](../Checks/), with [CheckWithPredicateEvaluation](../Checks/CheckWithPredicateEvaluation.prefab) and [CheckWithToggleChildren](../Checks/CheckToUnlockEnable.prefab)
  * , to gate in-world interaction events behind specific criteria

### Condition-Predicate Hook-Up

As an example, see below for a condition on a [CheckWithPredicateEvaluation](../Checks/CheckWithPredicateEvaluation.prefab) game object:

<img src="../../../InfoTools/Documentation/Game/Predicates/CheckWithPredicateEvaluationConfig.png" width="600">

This check interaction will only succeed if:
* either Frankie **OR** Tilly are present in the party
* **AND** the PhilRamen quest is completed
* **AND** someone in the party has a key to the ball pit in their knapsack

## Battle AI Predicates

The predicate system is also re-purposed in the context of the combat system as an input to enemy action selection.  For more detail, see [Battle AI](../Combat/BattleAI/).
