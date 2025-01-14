# Assets:  Game - Inventory (Items)

All inventory items occupy space in a character's [knapsack](../../../Scripts/Inventory/Knapsack.cs).  The different types of items have different purposes, as described below:

|              Item Type              |       |                   Purpose                    |       |                                             e.g                                             |
| :---------------------------------: | :---: | :------------------------------------------: | :---: | :-----------------------------------------------------------------------------------------: |
|    [ActionItems](./ActionItems/)    |       | can be **used**, both in-world and in-combat |   -   |                     for restoring health/AP, for damaging enemies, etc.                     |
| [EquipableItems](./EquipableItems/) |       |     can be **equipped** onto a character     |   -   |             for increasing a character's [stat](../../../Scripts/Stats/Stat.cs)             |
|       [KeyItems](./KeyItems/)       |       |    can be applied to **quest completion**    |   -   |                                    for story progression                                    |
|  [WearableItems](./WearableItems/)  |       |       can be **attached to a sprite**        |   -   | for character cosmetics and increasing a character's [stat](../../../Scripts/Stats/Stat.cs) |

Further detail on each type of item is provided in their corresponding directory linked above.
