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
| [Speech](./Speech/) | [DialogueBox](./Speech/DialogueBox.prefab) |       |    Presents simple text to the user    |
| | [DialogueOptionBox](./Speech/DialogueOptionBox.prefab) | | Presents text to the user with selectable options/choices (presented horizontally) |
| | [DialogueOptionBoxFixedVert…](./Speech/DialogueOptionBoxFixedVerticalOptions.prefab) | | Same as [DialogueOptionBox](./Speech/DialogueOptionBox.prefab), but with choices presented vertically |
| [World](./World/) | [WorldOptions](./World/WorldOptions.prefab) | | In-world options (access to inventory, equipment, skills, etc.) |
| | [EscapeMenu](./World/EscapeMenu.prefab) | | Escape menu choices (options menu, quit) |
| [StartScreen](./StartScreen/) | [StartMenu](./StartScreen/StartMenu.prefab) | | Game start choices (new/load game, options menu, quit) |
|  | [LoadMenu](./StartScreen/LoadMenu.prefab) | | Load game menu choices (select game file, back) |
|  | [OptionsMenu](./StartScreen/OptionsMenu.prefab) | | Player preferences settings (audio volume, display resolution, etc.) |
|  | [GameOverMenu](./StartScreen/GameOverMenu.prefab) | | Game over choices (load game, quit) |
|  | [GameWinMenu](./StartScreen/GameWinMenu.prefab) | | Game win text and choices |
| [Combat](./Combat/) | [CombatLog](./Combat/MainBattleEntities/CombatLog.prefab) | | Presents game combat information stream (based on combat events) |
|  | [CombatOptions](./Combat/MainBattleEntities/CombatOptions.prefab) | | High-level battle options (fight, run, inventory, stats) |
|  | [SkillSelection](./Combat/MainBattleEntities/SkillSelection.prefab) | | In-combat skill selection menu |
| [Abilities](./Abilities/) | [AbilitiesBox](./Abilities/AbilitiesBox.prefab) | | In-world skill selection menu (same as [SkillSelection](./Combat/MainBattleEntities/SkillSelection.prefab), but with added detail) |
| [Stats](./Stats/) | [StatusBox](./Stats/StatusBox.prefab) | | Presents character stats (health, [BaseStats](../../Scripts/Stats/BaseStats.cs), experience, etc.) for (in-party) selectable character |
| [Inventory](./Inventory/) | [InventoryBox](./Inventory/InventoryBox.prefab) | | Presents (usable/selectable) inventory items for (in-party) selectable characters |
|  | [EquipmentBox](./Inventory/EquipmentBox.prefab) | | Presents (usable/selectable) equipment slots & equipped items for (in-party) selectable characters |
|  | [ShopBox](./Inventory/ShopBox.prefab) | | Presents (purchasable) inventory items |
|  | [CashTransferBox](./Inventory/CashTransferBox.prefab) | | Bank-to-wallet cash transfer menu |
|  | [InventoryMoveBox](./Inventory/InventoryMoveBox.prefab) | | Duplicate [InventoryBox](./Inventory/InventoryBox.prefab) for transferring items from one (in-party) character to another |
|  | [EquipmentInventoryBox](./Inventory/EquipmentInventoryBox.prefab) | | Duplicate [InventoryBox](./Inventory/InventoryBox.prefab) for selecting equipable items from the [EquipmentBox](./Inventory/EquipmentBox.prefab)  |
|  | [InventoryShopBox](./Inventory/InventoryShopBox.prefab) | | Duplicate [InventoryBox](./Inventory/InventoryBox.prefab) for selecting an (in-party) character's knapsack for selling or purchasing an item |
|  | [InventorySwapBox](./Inventory/InventorySwapBox.prefab) | | Duplicate [InventoryBox](./Inventory/InventoryBox.prefab) for reconciling loot drops when inventory is full |

## Other Combat UI



## Utilities

…
