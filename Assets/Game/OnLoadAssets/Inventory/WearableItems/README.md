# Assets:  Game - Wearable Items

Wearable items are used by a character to change their visual appearance in the world, usually for specific in-world events or mini-games (e.g. such as carrying a [giant turkey leg](./GiantTurkeyLeg.asset) or riding in a [bumper car](./BumperCarA.asset) at the fall fair).  See, for example, the [BumperCarA](./BumperCarA.asset) wearable item below:

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/WearableItemExample.png" width="350">

Note that wearable items differ from [EquipableItems](../EquipableItems/) in that they A) impact the visual state of the character in-world, and B) they do **NOT** occupy equipment slots.  

## Wearable Items: Quick Start Guide

### Make the Wearable Item

1. Navigate to this [WearableItems](./) directory (or any sub-directories within)
2. Right click and select `Create` -> `Inventory` -> `Wearable Item`

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/NewItemMenu.png" width="500">

### Configure the Wearable Item

Set:
* The standard [Inventory Item](../) parameters
* Wearable Prefab: link to a [Wearable](../../../CharacterObjects/Wearables/) game object
* Is Unique: toggle `enable` to ensure only one of these objects can be spawned on a character at a time
  * alternatively, set false if it's OK to have multiple of these objects appear on the character

Wearables may have character [stat](../../../../Scripts/Stats/Stat.cs) modifiers, but these are defined on the [Wearable Prefab](../../../CharacterObjects/Wearables/), not on the wearable item that exists in the [knapsack](../../../../Scripts/Inventory/Knapsack.cs).

## Wearables: Practical Use

Above stated, the primary purpose of the wearable item is to spawn its Wearable Prefab (as [above](#configure-the-wearable-item)) onto the `AttachedObjects` transform that exists on each [playable character](../../../CharacterObjects/PCs/).  For example, the[BumperCarA](./BumperCarA.asset) wearable item, creates an instance of the [BumperCarA](../../../CharacterObjects/Wearables/BumperCars/BumperCarA.prefab) prefab as a game object, such as with Frankie below:

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/WearableInGame.png" width="800">
