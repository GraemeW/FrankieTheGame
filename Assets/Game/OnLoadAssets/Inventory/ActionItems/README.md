# Assets:  Game - Action Items

Action items may be used by a character in combat (i.e. instead of using a [skill](../../Skills/)) and in-world via the knapsack UI menu.  Like skills, they are effectively an encapsulation of [Battle Actions](../BattleActions/), with several additional properties.  See, for example, the [ChocolateCheeseCurds](./ChocolateCheeseCurds.asset) action item below:

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/ActionItemExample.png" width="300">

A high-level summary on Action Items, Battle Actions and their overall role in the combat system is described in [Game/Combat](../../Combat/).

## Action Items: Quick Start Guide

### Make the Action Item

1. Navigate to this [ActionItems](./) directory (or any sub-directories within)
2. Right click and select `Create` -> `Inventory` -> `Action Item`

<img src="../../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/NewItemMenu.png" width="500">

### Configure the Action Item

Set:
* The standard [Inventory Item](../) parameters
* Battle Action: to the paired battle action for the skill
  * See [Battle Actions](../../BattleActions/) in [OnLoadAssets](../../) for further detail on Battle Action construction
* Consumable: whether the item disappears after use, or is multi-use
