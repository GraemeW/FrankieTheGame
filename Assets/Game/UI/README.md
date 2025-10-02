# Assets:  Game - UI

## UI Canvases

There are three UI canvases employed in Frankie:
* [UICanvas](./World/UI%20Canvas.prefab):  The primary canvas used for all in-world UI elements (dialogue boxes, options menus, stat/ability screens, inventory, shops, equipment, etc.)
* [BattleCanvas](./Combat/Battle%20Canvas.prefab):  The secondary canvas used for instantiating and managing the battle UI during combat 
* [BackingCanvas](./World/BackingCanvas.prefab):  A simple blank canvas that sits behind the UI/Battle canvases to display a backing image (for use when game objects may not be present/painted on the world)

## UIBox UI Elements

The majority of UI elements make use of the [UIBox](../../Scripts/UI/UIBox/) abstract base class, which is a flexible system to display arbitrary information, with built-in capabilities for various standard 2D RPG functions (e.g. textscan, 1D/2D option selection, hand-off/call-back to other UI elements, etc.).  

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

As noted in [UI Canvases](#ui-canvases), the [BattleCanvas](./Combat/Battle%20Canvas.prefab) is used to instantiate and manage key battle UI elements, which is accomplished by listening to events from the [BattleController](../Controllers/Battle%20Controller.prefab).  The combat menus, options, logs and skill selection are UIBox variants, as described briefly [above](#uibox-ui-elements).  Several of these elements, in addition to other key combat UI elements, are shown below:

<img src="../../../InfoTools/Documentation/Game/UI/ExampleCombatUI.png" width="400">

### Character/Enemy Slides

Each character in the active party has a [CharacterSlide](./Combat/CharacterEnemySlides/CharacterSlide.prefab) that indicates their name, HP, AP and overall status.  The latter is accomplished via slide color highlights, as well as [StatusEffectBobbles](./Combat/CharacterEnemySlides/StatusEffectBobble.prefab) that appear on the character slide when the character is afflicted by a status.  Each character slide operates independently by listening for `stateAltered` events from their character's [CombatParticipant](../../Scripts/Combat/CombatParticipant/CombatParticipant.cs) class.  The character slide also has a circular-fill symbol at its top-right to show the current cooldown until the given character is allowed to act again.

*N.B.  The same character slides are also used in-world when the player opens the [WorldOptions](./World/WorldOptions.prefab).*

Each enemy in combat has an [EnemySlide](./Combat/CharacterEnemySlides/EnemySlide.prefab) that is used to display the enemy sprite.  When an enemy is defeated, their slide fades away and is then destroyed.  The enemy slide likewise displays [StatusEffectBobbles](./Combat/CharacterEnemySlides/StatusEffectBobble.prefab) for status afflictions, and has a circular-fill symbol at its bottom-left to indicate enemy cooldown.

### Damage Text and Custom Ability Effects

For simple skills/item effects (e.g. HP increase/decrease, AP increase/decrease, 'call for help', etc.) [DamageText](./Combat/DamageText/DamageText.prefab) UI elements are instantiated and then faded out on a given [CharacterSlide](./Combat/CharacterEnemySlides/CharacterSlide.prefab) or [EnemySlide](./Combat/CharacterEnemySlides/EnemySlide.prefab).  Slides are configured to listen for relevant events from their character's [CombatParticipant](../../Scripts/Combat/CombatParticipant/CombatParticipant.cs) class to automatically generate and configure instances of [DamageText](./Combat/DamageText/DamageText.prefab) via their child [DamageTextSpawners](./Combat/DamageText/DamageTextSpawner.prefab).  

More complicated visual effects are generally accomplished via [BattleEffectShaders](../VFX/Shaders/README.md#battle-effects) as part of the given skill/item itself.

## Utilities

In order to ensure maximum re-usability and to allow for global configurability, the UI elements described above are derived from the same UI building blocks, located in [Utilities](./Utilities/).  

For example a standard UI Window is comprised of:
* [Backing](./Utilities/Backing.prefab):  black background
* [Frame](./Utilities/Frame.prefab):  outer window frame, color-configurable (e.g. via PlayerPrefs)

, which is made into a dialogue box by adding:
* [LinkedSpeechEntry](./Utilities/LinkedSpeechEntry.prefab):  for displaying speech from a character
* [LinkedTextEntry](./Utilities/LinkedTextEntry.prefab):  for displaying descriptive text

, or made into a dialogue box with user selection, by further adding:
* [UIChoiceOption](./Utilities/UIChoiceButton.prefab):  for each independent option/choice (oriented horizontally)
  * , or [UIChoiceOptionVertical](./Utilities/UIChoiceButtonVertical.prefab):  if the options are oriented vertically

, or made into an option menu by adding:
* [StandardHeaderElement](./Utilities/StandardHeaderElement.prefab):  for a menu title
* [StandardTextElement](./Utilities/StandardTextElement.prefab):  for any display text
* [StandardConfirmationMenu](./Utilities/StandardConfirmationMenu.prefab):  for a simple accept/reject menu option
  * , or a [StandardEscapeMenu](./Utilities/StandardEscapeMenu.prefab):  For a simple reject/cancel menu option
