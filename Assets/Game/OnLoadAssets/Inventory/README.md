# Assets:  Game - Inventory (Items)

All inventory items occupy space in a character's [knapsack](../../../Scripts/Inventory/Knapsack.cs).  The different types of items have different purposes, as described below:

|              Item Type              |       |                   Purpose                    |       |                                             e.g                                             |
| :---------------------------------: | :---: | :------------------------------------------: | :---: | :-----------------------------------------------------------------------------------------: |
|    [ActionItems](./ActionItems/)    |       | can be **used**, both in-world and in-combat |   -   |                     for restoring health/AP, for damaging enemies, etc.                     |
| [EquipableItems](./EquipableItems/) |       |     can be **equipped** onto a character     |   -   |             for increasing a character's [stat](../../../Scripts/Stats/Stat.cs)             |
|       [KeyItems](./KeyItems/)       |       |    can be applied to **quest completion**    |   -   |                                    for story progression                                    |
|  [WearableItems](./WearableItems/)  |       |       can be **attached to a sprite**        |   -   | for character cosmetics and increasing a character's [stat](../../../Scripts/Stats/Stat.cs) |

All items share the below standard properties:
* Item ID: a unique [GUID](https://en.wikipedia.org/wiki/Universally_unique_identifier)
  * does NOT need to be input manually 
  * will be generated automatically when the asset is created & saved
* Display Name: the item name, as-seen in game UI
  * *note: the length of the name should be kept within reason*
* Description: the detailed item description can be viewed by inspecting the item in the knapsack, as below

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/ActionItemInspect.png" width="600">

* Droppable: whether the character is allowed to drop the item
  * impacting the 'drop' option being available in the UI, as above
  * generally more relevant for [key items](./KeyItems/) used in quests, but also possible for other types of items
* Price: base cost of the item
  * , which is further modified by vendor-specific scaling for both buying/selling

Further detail on each type of item is provided in their corresponding directory linked above.
