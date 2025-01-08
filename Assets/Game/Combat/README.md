# Assets: Game - Combat

A high-level summary of the combat / battle action system in Frankie is shown below:

<img src="../../../InfoTools/Documentation/Game/Combat/Frankie-BattleActions.png" width="450">

## Battle Actions

[BattleActions](../OnLoadAssets/BattleActions/) are the backbone of both [skills](#skills) and [action items](#action-items).  They are comprised of:
  1. [EffectStrategies](./BattleActions/EffectStrategies/): to adjust HP/AP, apply status effects, modify stats, override cooldown, call for help, etc.
       * *N.B. [EffectStrategiesSpawnedArtwork](./BattleActions/EffectStrategiesSpawnedArtwork/) are a special category of EffectStrategies that can be used to spawn artwork on the [BattleCanvas](../UI/Combat/Battle%20Canvas.prefab)*
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

### Action Items

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

### Skill Tree Construction

Skills are arranged in nodes, in positions:  up, down, left, right.

<img src="../../../InfoTools/Documentation/Game/Combat/SkillTreeRootNode.png" width="200">

As discussed in [Skills](#skills) above, each skill has a paired [stat](../../Scripts/Stats/Stat.cs).  As a character's relevant stat increases, they unlock branches to new nodes, and thus, new skills.  The starting skill node is referred to as the 'Root Node', and the full set of interlinked nodes defines the Skill Tree.

<img src="../../../InfoTools/Documentation/Game/Combat/SkillTreeBranched.png" width="400">

See the [SkillTrees](./SkillTrees/) directory for further detail, including a quick-start guide to make new skill tree scriptable objects.

### Skill Tree Navigation

In order to select individual skills, the player inputs a combination of directional commands to navigate through the skill tree.  Stronger skills are buried further in the skill tree, and so require a longer sequence of directional inputs.  

This is illustrated below:

<img src="../../../InfoTools/Documentation/Game/Combat/SkillTreePathExample.png" width="600">

For example:
* To access 'Gentle Praise', the player hits:  Up->Select
* To access 'Smile Menacingly', the player hits:  Up->Left->Select
* To access 'Words of Encouragement', the player hits:  Up->Left->Up->Select

, and finally:

* To access 'Wallop', the player hits:  Up->Left->Up->Right->Select

*N.B.  A character's stats can be modified during combat (e.g. due to status effects) -- thus granting or blocking a character's access to skills, nodes and entire sections of the skill tree.*

## Battle AI

Enemy NPCs navigate through the skill tree system using a bespoke AI system, where an arbitrary number of AI priorities can be placed in preference sequence.

AI priorities consist of:
* a list of skills
* conditions in which the skills will trigger
* target priorities

<img src="../../../InfoTools/Documentation/Game/Combat/BattleAIPredicateExample.png" width="350">

See the [BattleAI](./BattleAI/) directory for further detail, including a quick-start guide to make new AI  priority scriptable objects, with example battle AI setups.

*N.B. In the absence of AI priorities:*
* *skills are chosen at random, with a pre-defined probability to traverse deeper into the skill tree*
* *targets are chosen at random*
