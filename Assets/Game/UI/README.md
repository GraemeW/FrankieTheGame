# Assets:  Game - UI

## UI Roots

Frankie's UI is built in UI Toolkit, so there are no canvases.  Spawned UI is parented to one of two plain root objects:
* [WorldUIRoot](./Utilities/WorldUIRoot.prefab):  The parent for all in-world UI elements (dialogue boxes, options menus, stat/ability screens, inventory, shops, equipment, etc.)
* [BattleUI](./Combat/BattleUI.prefab):  The root of the battle UI during combat - wires up the battle stage (background, slides, effects), combat messages, combat options and skill selection 

## UI Toolkit Structure

Every UI element is a prefab with a `PanelRenderer` (drawing onto the shared [FrankiePanelSettings](./_UIToolkit/FrankiePanelSettings.asset)) and a view component from the [UIBox](../../../Packages/com.lowdefmustard.uibox/) package, with the following files sat next to the prefab:
* `<Name>.uxml`:  the element's layout (structure only - text and choices are filled in by code through bound models)
* `<Name>.uss`:  the element's styling, attached via the view component's style sheet list
* `<Name>Preview.uxml`:  a copy of the layout with sample content and linked style sheets, for viewing in UI Builder only (keep in sync by hand)

Shared styling lives in [_UIToolkit](./_UIToolkit/):  [Tokens.uss](./_UIToolkit/Styles/Tokens.uss) (the only home for asset references - fonts, images - and shared colours/sizes) and [UIBox.uss](./_UIToolkit/Styles/UIBox.uss) (frames, backings, text and choice entries), both applied to every panel through [FrankieTheme](./_UIToolkit/FrankieTheme.tss).

## UIBox UI Elements

The majority of UI elements make use of the [UIBox](../../../Packages/com.lowdefmustard.uibox/) abstract base class, which is a flexible system to display arbitrary information, with built-in capabilities for various standard 2D RPG functions (e.g. textscan, 1D/2D option selection, hand-off/call-back to other UI elements, etc.).  

The simplest example of the UIBox is the dialogue box, shown below:

<img src="../../../InfoTools/Documentation/Game/UI/ExampleUIBox-DialogueBox.png" width="400">

, but as noted above, the UIBox is regularly extended to display arbitrary information and choice selections, as below with the World Options and Status Boxes:

<img src="../../../InfoTools/Documentation/Game/UI/ExampleUIBox-WorldOptions-Status.png" width="400">

### Summary of UIBoxes

In order of (roughly) increasing categorical complexity:

|         Category          |                             UI Element                              |       |                                                                      Detail                                                                      |
|:-------------------------:|:-------------------------------------------------------------------:| :---: |:------------------------------------------------------------------------------------------------------------------------------------------------:|
|    [Speech](./Speech/)    |             [DialogueBox](./Speech/DialogueBox.prefab)              |       |                                                         Presents simple text to the user                                                         |
|                           |       [DialogueOptionBox](./Speech/DialogueOptionBox.prefab)        | |                                           Presents text to the user with selectable options/choices                                              |
| [MainMenus](./MainMenus/) |         [StartMenu](./MainMenus/StartMenu/StartMenu.prefab)         | |                                              Game start choices (new/load game, options menu, quit)                                              |
|                           |      [LoadMenu](./MainMenus/StartMenu/LoadGameMenu.prefab)          | |                                                 Load game menu choices (select game file, back)                                                  |
|                           |     [GameOverMenu](./MainMenus/GameWinLose/GameOverMenu.prefab)     | |                                                       Game over choices (load game, quit)                                                        |
|                           |      [GameWinMenu](./MainMenus/GameWinLose/GameWinMenu.prefab)      | |                                                            Game win text and choices                                                             |
|                           |        [OptionsMenu](./MainMenus/InWorld/OptionsMenu.prefab)        | |                                       Player preferences settings (audio volume, display resolution, etc.)                                       |
|                           |       [WorldOptions](./MainMenus/InWorld/WorldOptions.prefab)       | |                                         In-world options (access to inventory, equipment, skills, etc.)                                          |
|                           |         [EscapeMenu](./MainMenus/InWorld/EscapeMenu.prefab)         | |                                                     Escape menu choices (options menu, quit)                                                     |
|                           |  [CombatOptions](./Combat/MainBattleEntities/CombatOptions.prefab)  | |                                             High-level battle options (fight, run, inventory, stats)                                             |
|                           | [SkillSelection](./Combat/MainBattleEntities/SkillSelection.prefab) | |                                                          In-combat skill selection menu                                                          |
| [Abilities](./Abilities/) |           [AbilitiesBox](./Abilities/AbilitiesBox.prefab)           | |        In-world skill selection menu (same as [SkillSelection](./Combat/MainBattleEntities/SkillSelection.prefab), but with added detail)        |
|      [Map](../Map/)       |                 [MapSuper](../Map/MapSuper.prefab)                  | |                       Presents a snapshot of the current (exterior) zone with the player's location, per [Map](../Map/README.md)                       |
|     [Stats](./Stats/)     |                [StatusBox](./Stats/StatusBox.prefab)                | |      Presents character stats (health, [BaseStats](../../Scripts/Stats/BaseStats.cs), experience, etc.) for (in-party) selectable character      |
| [Inventory](./Inventory/) |           [InventoryBox](./Inventory/InventoryBox.prefab)           | |                                Presents (usable/selectable) inventory items for (in-party) selectable characters                                 |
|                           |           [EquipmentBox](./Inventory/EquipmentBox.prefab)           | |                        Presents (usable/selectable) equipment slots & equipped items for (in-party) selectable characters                        |
|                           |                [ShopBox](./Inventory/ShopBox.prefab)                | |                                                      Presents (purchasable) inventory items                                                      |
|                           |        [CashTransferBox](./Inventory/CashTransferBox.prefab)        | |                                                        Bank-to-wallet cash transfer menu                                                         |
|                           |       [InventoryMoveBox](./Inventory/InventoryMoveBox.prefab)       | |            Duplicate [InventoryBox](./Inventory/InventoryBox.prefab) for transferring items from one (in-party) character to another             |
|                           |  [EquipmentInventoryBox](./Inventory/EquipmentInventoryBox.prefab)  | | Duplicate [InventoryBox](./Inventory/InventoryBox.prefab) for selecting equipable items from the [EquipmentBox](./Inventory/EquipmentBox.prefab) |
|                           |       [InventoryShopBox](./Inventory/InventoryShopBox.prefab)       | |   Duplicate [InventoryBox](./Inventory/InventoryBox.prefab) for selecting an (in-party) character's knapsack for selling or purchasing an item   |
|                           |       [InventorySwapBox](./Inventory/InventorySwapBox.prefab)       | |                   Duplicate [InventoryBox](./Inventory/InventoryBox.prefab) for reconciling loot drops when inventory is full                    |

## Display-Only UI

A few full-screen elements show information without taking input, and so are not UIBoxes (they reuse the same view components and bound models):

|                                    UI Element                                    |                                                               Detail                                                                |
|:--------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------------------------------------------------------:|
|                     [WalletUI](./Inventory/WalletUI.prefab)                      |                                           Presents the party's cash alongside other boxes                                            |
|                  [Fader](../Core/CoreDep/Fader/Fader.prefab)                     | Full-screen fades for zone / battle transitions, drawn on its own top-sorted [FaderPanelSettings](./_UIToolkit/FaderPanelSettings.asset) |
| [CinematicOverlay](../Cinematics/StandardCinematicGameObjects/CinematicOverlay.prefab) |                     Full-screen text card for cinematics, with its opacity keyed from Timeline (previews in edit mode)                      |
|               [SplashScreen](./MainMenus/Splash/SplashScreen.uxml)               |                                      Studio and title cards shown on launch (lives in the splash scene)                                      |

## Other Combat UI

As noted in [UI Roots](#ui-roots), the [BattleUI](./Combat/BattleUI.prefab) is used to instantiate and manage key battle UI elements, which is accomplished by listening to events from the [BattleController](../Controllers/Battle%20Controller.prefab).  The combat menus, options, logs and skill selection are UIBox variants, as described briefly [above](#uibox-ui-elements).  Several of these elements, in addition to other key combat UI elements, are shown below:

<img src="../../../InfoTools/Documentation/Game/UI/ExampleCombatUI.png" width="400">

### Character/Enemy Slides

Each character in the active party has a [CharacterSlide](./Combat/CharacterEnemySlides/CharacterSlide.uss) that indicates their name, HP, AP and overall status.  The latter is accomplished via slide color highlights, as well as StatusEffectBobbles that appear on the character slide when the character is afflicted by a status.  Each character slide operates independently by listening for `stateAltered` events from their character's [CombatParticipant](../../Scripts/Combat/CombatParticipant/CombatParticipant.cs) class.  The character slide also has a circular-fill symbol at its top-right to show the current cooldown until the given character is allowed to act again.

*N.B.  The same character slides are also used in-world when the player opens the [WorldOptions](./MainMenus/InWorld/WorldOptions.prefab).*

Each enemy in combat has an [EnemySlide](./Combat/CharacterEnemySlides/EnemySlide.uss) that is used to display the enemy sprite.  When an enemy is defeated, their slide fades away and is then destroyed.  The enemy slide likewise displays StatusEffectBobbles for status afflictions, and has a circular-fill symbol at its bottom-left to indicate enemy cooldown.

### Damage Text and Custom Ability Effects

For simple skills/item effects (e.g. HP increase/decrease, AP increase/decrease, 'call for help', etc.) DamageText UI elements are instantiated and then faded out on a given CharacterSlide or EnemySlide.  Slides are configured to listen for relevant events from their character's [CombatParticipant](../../Scripts/Combat/CombatParticipant/CombatParticipant.cs) class to automatically generate and configure instances of DamageText via their DamageTextSpawners.  

More complicated visual effects are generally accomplished via [BattleEffectShaders](../VFX/Shaders/README.md#battle-effects) as part of the given skill/item itself.
