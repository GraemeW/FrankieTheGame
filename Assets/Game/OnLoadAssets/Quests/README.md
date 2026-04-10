# Assets:  Game - Quests

Quests are simple scriptable objects with brief detail on tasks that Frankie may complete.  Briefly, a [QuestGiver](../../../Scripts/Quests/QuestGiver.cs) will assign a quest, and after certain conditions are met (e.g. foe defeated, item delivered, etc.), a [QuestCompleter](../../../Scripts/Quests/QuestCompleters/QuestCompleter.cs) will mark the quest completed in the [QuestList](../../../Scripts/Quests/QuestList.cs) & disburse any relevant rewards/loot.

## Quests: Quick Start Guide

### Make the Quest

1. Navigate to this [Quests](./) directory (or any sub-directories within)
2. Right click and select `Create` -> `Quests` -> `New Quest`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/NewQuestMenu.png" width="500">

### Configure the Quest

Set:
* Detail:  brief description of the quest
* Quest Objective Names:
  * Populate with as many sub-quest objectives as needed
  * Provide each sub-quest with a brief title
* Rewards (if applicable)
  * Hook up any [InventoryItems](../Inventory/) to be transferred to Frankie upon completion of the quest

*Note:  QuestID can be ignored, as it is an auto-generated GUID*

After setting the quest objectives, click the `Generate Objectives` button to create the sub-objective assets, and then save the asset.

An example quest with several objectives is shown below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/HorseQuestExample.png" width="300">

, with the resultant scriptable object && childed sub-quest objects:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/HorseQuestScriptableObject.png" width="200">

## Quest Integrations

Quests are the primary method in which we track Frankie's journey through the game.  As discussed in further detail in [Predicates](../../Predicates/), quests are hooked into numerous other systems via [QuestList Predicates](../../Predicates/QuestList/), where we can evaluate if quest objectives or entire quests have been completed.  

### Example Quest Flow

Quest integration is shown in a mini-quest below, where Frankie needs to talk to the Hipster Office Worker:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/HipsterQuestExample.png" width="300">

If we examine the Hipster Office Worker, we can see he has both a Quest Giver and Quest Completer script hooked up to the relevant quest / objective scriptable object above:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/HipsterQuestGiverCompleter.png" width="500">

Then:
* the Quest Giver component is triggered via dialogue initiation, as seen on the Hipster Office Workers ConversantComponent:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/HipsterQuestDialogueInitiated.png" width="300">

* likewise, the Quest Completer component is triggered via dialogue completion:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/HipsterQuestDialogueCompleted.png" width="300">

* a simple 'Quest Objective Completed` predicate tracks if the quest objective has been completed:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/HipsterQuestPredicate.png" width="300">

* , and the same predicate is used to modify the Hipster Office Worker's dialogue tree:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/HipsterDialogueVsQuestPredicate.png" width="500">

*N.B.*
* *Further detail on the predicate system is provided in [Predicates](../../Predicates/)*
* *Further detail on the dialogue system is provided in [Speech](../../Speech/)*

### Quest Completion via Key Items

In addition to being completed via the Quest Completer component, quest objectives may also be completed simply by possessing a key item in any of the party characters' knapsacks.  This behavior is discussed further in [KeyItems](../Inventory/KeyItems/).

## Quests as Game State Modifiers

Quests implement the [GameStateModifier](../../../Scripts/Core/GameStateModifiers/GameStateModifier.cs) abstract base class, indicating that they can alter the game state in a volatile/unrecoverable manner.

As a Game State Modifier, the following custom editor behaviours are in effect:

### Tracking Handlers that Modify Quests

When selecting a Quest scriptable object in Unity, its inspector properties show a list of game objects across the project that both A) implement the [IGameStateModifierHandler](../../../Scripts/Core/GameStateModifiers/IGameStateModifierHandler.cs) interface and B) reference the selected Quest scriptable object.  This interface indicates that these game objects can modify the quest state (e.g. such as by giving quests or completing quest objectives).  These handlers are automatically linked to the Quest whenever its scriptable object is referenced by the corresponding [IGameStateModifierHandler](../../../Scripts/Core/GameStateModifiers/IGameStateModifierHandler.cs) via serialization callbacks.

As an example, see below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/CustomEditorHandlerData.png" width="300">

Linked handlers are noted with corresponding `Open & Select` buttons, which can be pressed to open the relevant scene and select the specific game object. 

### Editor Gizmos for Game State Modifier Handlers

When viewing any [IGameStateModifierHandler](../../../Scripts/Core/GameStateModifiers/IGameStateModifierHandler.cs) in the editor that is currently configured with a valid GameStateModifier (such as a Quest or a Key Item), a Gizmo marker will be drawn at its GameObject location.  The default gizmo is a red circle with a yellow star, as below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Quests/CustomEditorHandlerGizmos.png" width="350">
