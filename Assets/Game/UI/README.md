# Assets:  Game - UI

## UI Canvases

There are three UI canvases employed in Frankie:
* [UICanvas](./World/UI%20Canvas.prefab):  The primary canvas used for all in-world UI elements (dialogue boxes, options menus, stat/ability screens, inventory, shops, equipment, etc.)
* [BackingCanvas](./World/BackingCanvas.prefab):  A simple blank canvas that sits behind the UI canvas to display a backing image (for use when game objects may not be present/painted on the world)
* [BattleCanvas](./Combat/Battle%20Canvas.prefab):  The secondary canvas used for displaying the battle UI during combat 

## UIBox UI Elements

The majority of UI elements make use of the [UIBox](../../Scripts/Utils/UIBox/) abstract base class, which is a flexible system to display arbitrary information, with built-in capabilities for various standard 2D RPG functions (e.g. textscan, 1D/2D option selection, hand-off/call-back to other UI elements, etc.).  

The simplest example of the UIBox is the dialogue box, shown below:

<img src="../../../InfoTools/Documentation/Game/UI/ExampleUIBox-DialogueBox.png" width="400">

, but as noted above, the UIBox is regularly extended to display arbitrary information and choice selections, as below with the World Options and Status Boxes:

<img src="../../../InfoTools/Documentation/Game/UI/ExampleUIBox-WorldOptions-Status.png" width="400">

### Summary of UIBoxes

In order of (roughly) increasing categorical complexity:

|      Category       |                 UI Element                 |       | Detail |
| :-----------------: | :----------------------------------------: | :---: | :------------: |
| [Speech](./Speech/) | [DialogueBox](./Speech/DialogueBox.prefab) |       |    Simple text to the user    |
| | [DialogueOptionBox](./Speech/DialogueOptionBox.prefab) | | Text to the user with selectable options/choices (presented horizontally) |
| | [DialogueOptionBoxFixedVert…](./Speech/DialogueOptionBoxFixedVerticalOptions.prefab) | | Same as [DialogueOptionBox], but with choices presented vertically |
| [World](./World/) | [WorldOptions](./World/WorldOptions.prefab) | | |
| | [EscapeMenu](./World/EscapeMenu.prefab) | | |
| [StartScreen](./StartScreen/) | [StartMenu](./StartScreen/StartMenu.prefab) | | |
|  | [LoadMenu](./StartScreen/LoadMenu.prefab) | | |
|  | [OptionsMenu](./StartScreen/OptionsMenu.prefab) | | |
|  | [GameOverMenu](./StartScreen/GameOverMenu.prefab) | | |
|  | [GameWinMenu](./StartScreen/GameWinMenu.prefab) | | |
| [Combat](./Combat/) | [CombatLog](./Combat/MainBattleEntities/CombatLog.prefab) | | |
|  | [CombatOptions](./Combat/MainBattleEntities/CombatOptions.prefab) | | |
|  | [SkillSelection](./Combat/MainBattleEntities/SkillSelection.prefab) | | |
| [Abilities](./Abilities/) | [AbilitiesBox](./Abilities/AbilitiesBox.prefab) | | |
| [Stats](./Stats/) | [StatusBox](./Stats/StatusBox.prefab) | | |
| [Inventory](./Stats/) | [InventoryBox](./Inventory/InventoryBox.prefab) | | |
|  | [EquipmentBox](./Inventory/EquipmentBox.prefab) | | |
|  | [ShopBox](./Inventory/ShopBox.prefab) | | |
|  | [CashTransferBox](./Inventory/CashTransferBox.prefab) | | |
|  | [InventoryMoveBox](./Inventory/InventoryMoveBox.prefab) | | |
|  | [EquipmentInventoryBox](./Inventory/EquipmentInventoryBox.prefab) | | |
|  | [InventoryShopBox](./Inventory/InventoryShopBox.prefab) | | |
|  | [InventorySwapBox](./Inventory/InventorySwapBox.prefab) | | |

## Other Combat UI

…

## Utilities

…
