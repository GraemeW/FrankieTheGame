# Assets: Game - Combat

A high-level summary of the Battle Action system in Frankie is shown below:

<img src="../../../InfoTools/Documentation/Game/Combat/Frankie-BattleActions.png" width="450">

## Battle Actions

[BattleActions](../OnLoadAssets/BattleActions/) are the backbone of both the [skill](#skills) and [inventory](#inventory) systems.  They are comprised of:
  1. [EffectStrategies](./BattleActions/EffectStrategies/): to adjust HP/AP, apply status effects, modify stats, override cooldown, call for help, etc.
    * [EffectStrategiesSpawnedArtwork](./BattleActions/EffectStrategiesSpawnedArtwork/) are a special category of EffectStrategies that can be used to spawn visual feedback/artwork on the [BattleCanvas](../UI/Combat/Battle%20Canvas.prefab)
  2. [TargetingStrategies](./BattleActions/TargetingStrategies/): to set the active target type (single vs. multi, enemy vs. ally, etc.)
    * , where [FilterStrategies](./BattleActions/FilterStrategies/) are used to further narrow the targeting strategy - e.g. to single characters

<img src="../../../InfoTools/Documentation/Game/Combat/BattleActionExample.png" width="350">

See the [BattleActions](../OnLoadAssets/BattleActions/) directory in [OnLoadAssets](../OnLoadAssets/) for further detail, including a quick-start guide to make new BattleAction scriptable objects.

## Skills

[Skills](../OnLoadAssets/Skills/) are used by both [PCs](../CharacterObjects/PCs/) and [NPCs](../CharacterObjects/NPCs/).  For PCs, they may be used in either combat or in world.  They are comprised of:
1. a [battle action](#battle-actions)
2. a paired [stat](../../Scripts/Stats/Stat.cs)
3. 'detail' AKA flavour text, which appears in the [Abilities UI menu](../UI/Abilities/AbilitiesBox.prefab)

<img src="../../../InfoTools/Documentation/Game/Combat/SkillExample.png" width="350">

See the [Skills](../OnLoadAssets/Skills/) directory in [OnLoadAssets](../OnLoadAssets/) for further detail, including a quick-start guide to make new Skills scriptable objects.

## Inventory Items

[Inventory Items](../OnLoadAssets/Inventory/) are used only by [PCs](../CharacterObjects/PCs/).  There are several different flavours of inventory items, including:
* [ActionItems](../OnLoadAssets/Inventory/ActionItems/): standard items kept in the player's [knapsack](../../Scripts/Inventory/Knapsack.cs), and may be **used** in either combat or in world
* [EquipableItems](../OnLoadAssets/Inventory/EquipableItems/): items that cannot be used, but instead may be [equipped](../../Scripts/Inventory/Equipment.cs) onto the PC to alter its stats
* [KeyItems](../OnLoadAssets/Inventory/KeyItems/): items that cannot be used, but may be required for [quest objectives](../OnLoadAssets/Quests/Quests/)
* [WearableItems](../OnLoadAssets/Inventory/WearableItems/): items that are worn to alter the appearance of the player in world

[ActionItems](../OnLoadAssets/Inventory/ActionItems/) are closest in spirit to skills, with the following properties:
1. a [battle action](#battle-actions)
1. a display name, as seen in inventory / shops
2. a description, for further inspection
3. boolean toggles for:
     * droppable - i.e. can it be removed from inventory
     * consumable - i.e. does it disappear after use
4. a price, for purchase in [shops](../../Scripts/Inventory/Shop.cs)

<img src="../../../InfoTools/Documentation/Game/Combat/InventoryItemExample.png" width="350">

See the [Inventory](../OnLoadAssets/Inventory/) directory in [OnLoadAssets](../OnLoadAssets/) for further detail, including a quick-start guide to make new inventory item scriptable objects.

## Skill Trees



## Skill Handler / Battle AI
