# Assets:  Game - Action Items

Action items may be used by a character in combat (i.e. instead of using a [skill](../../Skills/)) and in-world via the knapsack UI menu.  Like skills, they are effectively an encapsulation of [Battle Actions](../BattleActions/), with several additional properties.  See, for example, the `Chocolate Cheese Curds` action item below:

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/ActionItemExample.png" width="300">

A high-level summary on Action Items, Battle Actions and their overall role in the combat system is described in [Game/Combat](../../Combat/).

## Action Items: Quick Start Guide

### Make the Action Item

1. Navigate to this [ActionItems](./) directory (or any sub-directories within)
2. Right click and select `Create` -> `Inventory` -> `Action Item`

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/NewItemMenu.png" width="500">

### Configure the Action Item

Set:
* Battle Action: to the paired battle action for the skill
  * See [Battle Actions](../../BattleActions/) in [OnLoadAssets](../../) for further detail on Battle Action construction
* Display Name: the item name, as-seen in game UI
  * *note: the length of the name should be kept within reason*
* Description: the detailed item description can be viewed by inspecting the item in the knapsack, as below

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/ActionItemInspect.png" width="600">

* Droppable: whether the character is allowed to drop the item
  * impacting the 'drop' option being available in the UI, as above
  * generally more relevant for key items used in quests, but also possible for standard action items
* Price: base cost of the item
  * , which is further modified by vendor-specific scaling for both buying/selling
* Consumable: whether the item disappears after use, or is multi-use
