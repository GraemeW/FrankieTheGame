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

## Inventory Items as Game State Modifiers

Inventory items implement the [GameStateModifier](../../../Scripts/Core/GameStateModifiers/GameStateModifier.cs) abstract base class, indicating that they can alter the game state in a volatile/unrecoverable manner.  

As a Game State Modifier, several custom editor behaviours are in effect, described further below. 

_N.B.  While all Inventory Items implement [GameStateModifier](../../../Scripts/Core/GameStateModifiers/GameStateModifier.cs), only [KeyItems](./KeyItems) currently trigger the custom editor behaviour noted below (this may be changed in the future)_ 

### Tracking Handlers that Modify Key Items

When selecting a KeyItem scriptable object in Unity, its inspector properties show a list of game objects across the project that both A) implement the [IGameStateModifierHandler](../../../Scripts/Core/GameStateModifiers/IGameStateModifierHandler.cs) interface and B) reference the selected KeyItem scriptable object.  This interface indicates that these game objects can add or remove the KeyItem from the player's characters' inventory.  These handlers are automatically linked to the KeyItem whenever its scriptable object is referenced by the corresponding [IGameStateModifierHandler](../../../Scripts/Core/GameStateModifiers/IGameStateModifierHandler.cs) via serialization callbacks.

As an example, see below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/CustomEditorHandlerData.png" width="300">

Linked handlers are noted with corresponding `Open & Select` buttons, which can be pressed to open the relevant scene and select the specific game object.

### Editor Gizmos for Game State Modifier Handlers

When viewing any [IGameStateModifierHandler](../../../Scripts/Core/GameStateModifiers/IGameStateModifierHandler.cs) in the editor that is currently configured with a valid GameStateModifier (such as a Quest or a Key Item), a Gizmo marker will be drawn at its GameObject location.  The default gizmo is a red circle with a yellow star, as below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Inventory/CustomEditorHandlerGizmos.png" width="600">

