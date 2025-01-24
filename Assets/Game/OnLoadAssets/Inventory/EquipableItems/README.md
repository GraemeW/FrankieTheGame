# Assets:  Game - Equipable Items

Equipable items may be equipped by a character into one of four equipment slots, defined by [EquipLocation](../../../../Scripts/Inventory/EquipLocation.cs):
* Hands
* Arms
* Neck
* Other

Generally, equipment are used to bolster a character's [Stats](../../../../Scripts/Stats/Stat.cs) in order to improve their prowess in combat or to gain access to additional [Skills](../../Skills/).  Equipable items may be restricted to specific [characters](../../../CharacterObjects/) via conditional predicates. See, for example, the [OneFootRuler](./OneFootRuler.asset):

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/EquipableItemExample.png" width="350">

## Equipable Items: Quick Start Guide

### Make the Equipable Item

1. Navigate to this [EquipableItems](./) directory (or any sub-directories within)
2. Right click and select `Create` -> `Inventory` -> `Equipable Item`

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/NewItemMenu.png" width="500">

### Configure the Equipable Item

Set:
* The standard [Inventory Item](../) parameters
* Equip Location: the equipment slot / location the item can be equipped into (as above)
* Base Stat Modifiers: the stats to adjust on the character
  * can be negative or positive
  * min/max dictate a range from which a random value can be selected each time the stat is accessed
* Condition: follows the logic defined in [GamePredicates](../../../Predicates/)
  * using a [conjunctive normal form](https://en.wikipedia.org/wiki/Conjunctive_normal_form) (CNF) of predicate boolean evaluators to check for some arbitrary game condition
  * in the example above, the predicate [IsTilly](../../../Predicates/BaseStats/CharacterChecks/IsTilly.asset) ensures only [Tilly](../../../CharacterObjects/PCs/Tilly/Tilly.prefab) can use the [OneFootRuler](./OneFootRuler.asset)
