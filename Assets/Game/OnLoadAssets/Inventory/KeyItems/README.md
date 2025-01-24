# Assets:  Game - Key Items

Key items are a special class of items that are kept in a character's [knapsack](../../../../Scripts/Inventory/Knapsack.cs) in order to trigger story beats, unlock dialogue options and complete specific quest objectives via the [Predicates](../../../Predicates/) system.  See, for example, the [XTremeBurnHotSauce](./XTremeBurnHotSauce.asset):

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/KeyItemExample.png" width="350">

## Key Items: Quick Start Guide

### Make the Key Item

1. Navigate to this [KeyItems](./) directory (or any sub-directories within)
2. Right click and select `Create` -> `Inventory` -> `Key Item`

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/NewItemMenu.png" width="500">

### Configure the Key Item

Set:
* The standard [Inventory Item](../) parameters
* Any relevant quest objectives from [Quests](../../Quests/)
  * in the above [XTremeBurnHotSauce](./XTremeBurnHotSauce.asset) example, the attached quest objective is `PickedUpHotSauce` from the [OfficeInteriorRamen](../../Quests/OfficeInteriorRamen.asset) quest

When a quest objective is attached to the key item (as above), it will be **completed** in the player's [Quest List](../../../../Scripts/Quests/QuestList.cs) as long as sed item remains in any active party member's inventory.  In this manner, one may use any [QuestObjectiveCompletedPredicate](../../../../Scripts/Predicates/QuestListPredicates/QuestObjectiveCompletedPredicate.cs) to conditionally trigger or toggle world events, objects, dialogue options, etc.  Continuing the above example, completion of `PickedUpHotSauce` is checked via the predicate [QuestObjCompleted_PickedUpHotSauce](../../../Predicates/QuestList/OfficeInterior/QuestObjCompleted_PickedUpHotSauce.asset).

Alternatively, one may also use the [HasItemPredicate](../../../../Scripts/Predicates/KnapsackPredicates/HasItemPredicate.cs) to directly evaluate for the presence of an item in a character's inventory, such as with the predicate [HasItemVeryImportantDocuments](../../../Predicates/Knapsack/HasItemVeryImportantDocuments.asset).
