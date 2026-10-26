# Assets:  Game - Battle Actions

Battle actions may be taken by characters in Frankie to deal damage, heal, apply status effects, call for help, etc. They are used by both [Skills](../Skills/) and [Action Items](../Inventory/ActionItems/). See, for example, the battle action for single target damage below:

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleActionExample.png" width="350">


A high-level summary on Battle Actions and their overall role in the combat system is described in [Game/Combat](../../Combat/).

## Battle Actions: Quick Start Guide

_N.B.  This quick start guide provides a summary on creating new battle actions using the default Unity editor for step-wise manual configuration.  This provide a reasonable high-level view of the construction battle action.  This stated, for most battle action authoring, one should make use of the [Battle Action Editor](#battle-action-editor)._

### Make the Battle Action

1. Navigate to the owner's sub-directory:  `Skills/` or `ActionItems/` (named after the skill / item that uses it), or `MiscUtility/` for shared utility actions (e.g. `DoNothing`)
   * or, use `New Battle Action` in the [Battle Action Editor](#battle-action-editor)
2. Right click and select `Create` -> `BattleAction` -> `New Battle Action`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/NewBattleActionMenu.png" width="450">

### Base Parameters

Set the `Other Input` parameters:
* Damage Type: either `Physical` or `Magical`, which notably selects for the defense stats opposed to the skill
* Cooldown: in seconds
* AP Cost: optional (set to 0 if no cost)

The [Targeting Strategy](#targeting-strategy) and [Effect Strategies](#effect-strategy) are described in more detail below.

### Targeting Strategy

To make a new targeting strategy:

1. Navigate to [Combat/BattleActions/TargetingStrategies](../../Combat/BattleActions/TargetingStrategies/)
   * depending on the complexity of the targeting strategy, navigate further to [SpecificTargeting](../../Combat/BattleActions/TargetingStrategies/SpecificTargeting/)
2. Right click and select `Create` -> `BattleAction` -> `Targeting` -> `Single Target` (or the relevant targeting type)

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/NewTargetingMenu.png" width="550">

The targeting strategy sets the rules on which target a skill **can** select, but the actual skill and target selection are done in combat via [SkillHandler](../../../Scripts/Combat/Skills/SkillHandler.cs) (for the PCs) or via the [BattleAI](../../../Scripts/Combat/BattleAI/BattleAI.cs) for the NPCs. 

#### Basic Targeting

Targeting strategies all derive from the [TargetingStrategy](../../../Scripts/Combat/BattleAction/Targeting/TargetingStrategy.cs) abstract class, and include:
* a parameter to select combat participant type: friendly, foe, or both
* [filter strategies](#filtering-strategy) to further narrow down the target

Friendly/foe description are relative to the character's disposition to the player.  The characters in a player's party are friendly to each other, and see enemies in combat as foes.  Likewise, an enemy is friendly to other enemies in combat, and sees the player characters as foes.

In the above example, the targeting strategy is [SingleTargetLivingEnemies](../../Combat/BattleActions/TargetingStrategies/AllTargetLivingFoes.asset), which means the skill can only target a single character in opposition to the active character's disposition.  This targeting strategy furthermore applies a `LivingFilter`, such that it can only be applied to combat participants that are alive (i.e. hit points > 0).

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/TargetingSingleEnemy.png" width="350">

#### Single vs. Multi Targeting

The above example covers the simple [Single Target](../../../Scripts/Combat/BattleAction/Targeting/SingleTargeting.cs) strategy.  However, many skills impact more than one character, and are instead handled by [TargetAll](../../../Scripts/Combat/BattleAction/Targeting/TargetAll.cs), [RowTargeting](../../../Scripts/Combat/BattleAction/Targeting/ColumnTargeting.cs), [ColumnTargeting](../../../Scripts/Combat/BattleAction/Targeting/ColumnTargeting.cs), etc.

See for example Trample:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/BattleActionTrample.png" width="350">

, which applies a large health deduction to all enemies, by making use of the [AllTargetLivingEnemies](../../Combat/BattleActions/TargetingStrategies/AllTargetLivingFoes.asset) targeting strategy:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/TargetingMultiEnemies.png" width="350">

Notably, the two new parameters introduced by multitargeting are:
* Number Of Enemies To Hit
* Override To Hit Everything

The override to hit everything parameter will ignore the number of enemies to hit and simply hit all the relevant entities on the screen.  Note though that filtering strategies **will still be applied**.

### Filtering Strategy

To make a new filtering strategy:
1. Navigate to [Combat/BattleActions/FilteringStrategies](../../Combat/BattleActions/FilterStrategies/)
2. Right click and select `Create` -> `BattleAction` -> `Filters` -> `Living` (or) `Character`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/NewFilteringMenu.png" width="550">

Filter strategies derive from the [FilterStrategy](../../../Scripts/Combat/BattleAction/Filtering/FilterStrategy.cs) abstract class, and simply reduce an IEnumerable of Battle Entities based on some pre-defined logic.  As above, the most basic types of filtering are:
* Living Filters: to select combat participants that are either alive or dead
  * see for example [LivingFilter](../../Combat/BattleActions/FilterStrategies/LivingFilter.asset)
* Character Filters: to select specific combat participants via their [CharacterProperties](../CharacterProperties/) asset
  * see for example [FrankieFilter](../../Combat/BattleActions/FilterStrategies/FrankieFilter.asset)

Alternate filtering strategies may be envisioned and built up in the future to arbitrarily select out targets as needed (e.g. via status effect, item in knapsack, etc.).

### Effect Strategy

To make a new effect strategy:
1. Navigate to [Combat/BattleActions/EffectStrategies](../../Combat/BattleActions/EffectStrategies/)
   * depending on the complexity of the targeting strategy, navigate further to the relevant subfolder
2. Right click and select `Create` -> `BattleAction` -> `Effects` -> `effect you desire`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/NewEffectMenu.png" width="650">

Effect strategies derive from the [EffectStrategy](../../../Scripts/Combat/BattleAction/StandardEffects/EffectStrategy.cs) abstract class, which requires a method to initiate the effect and a callback for when the effect is finished.  

#### Basic Effects

Some example basic effects include:
   * health effect: deal damage or heal - such as [DeductHP_Med](../../Combat/BattleActions/EffectStrategies/DirectHP/DeductHP_4j3_2xCrit.asset)
     * health is modified by Health Change ± Jitter
     * apply damage type selects if physical/magical defense should be used in damage calculations

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectDeductHP.png" width="250">

   * ap effect: add/remove action points from the character - such as [RestoreAP_Small](../../Combat/BattleActions/EffectStrategies/DirectAP/RestoreAP_3j3.asset)
     * action points are modified by AP Change ± Jitter

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectRestoreAP.png" width="250">

   * persistent stat effect: increment or decrement a [stat](../../../Scripts/Stats/Stat.cs) temporarily - such as [IncreaseLuck_Med](../../Combat/BattleActions/EffectStrategies/PersistentStatMods/IncreaseLuck_12.asset)
     * fraction probability sets the likelihood of effect applying (where 1 = 100%, 0 = 0%) on skill use
     * persist after combat defines whether the effect clears automatically after combat, or remains until duration expires

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectPersistentStat.png" width="250">

   * dot/hot effect: damage or heal over time - such as [DoTHPSmall](../../Combat/BattleActions/EffectStrategies/DoT/DoTHP_2p3s_over12s.asset)
     * health is modified by health change per teck every tick period seconds
     * fraction probability & persist match are consistent with the above explanation ^

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectDamageOverTime.png" width="250">
   
   * remove persistent stat effects: to clear the above stat/dot/hots - such as [ClearStatus_All](../../Combat/BattleActions/EffectStrategies/OtherHealing/ClearStatus_All.asset)
     * remove persistent recurring applies to HoT/DoTs
     * remove persistent stat refers to persistent stat effects
     * fraction probability defines chance of success to remove the stat
     * number of effects = 0 will remove all effects, otherwise removes number indicated

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectRemoveStatus.png" width="375">

   * permanent stat effect: increment or decrement a [stat](../../../Scripts/Stats/Stat.cs) permanently - such as [PermanentIncreaseBeauty_Large](../../Combat/BattleActions/EffectStrategies/PermanentStatMods/PermIncreaseBeauty_15.asset) 
   
<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectPermanentStat.png" width="250">

   * set cooldown: overrides the cooldown on the target (e.g. to delay enemy actions) - such as [SetCooldown_Long](../../Combat/BattleActions/EffectStrategies/SetCooldown/SetCooldown_8s.asset)

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectSetCooldown.png" width="250">

   * call for help effect: add new enemies to the current combat - such as [CallForHelp_Squirrel](../../Combat/BattleActions/EffectStrategies/CallForHelp/CallForHelp_Squirrel.asset)
     * , using the same logic as [EnemySpawner](../../WorldObjects/_Spawners/) configurations
     * , where a variable number of enemies (defined by their [Character Properties](../CharacterProperties/)) can be added with a given probability

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectCallForHelp.png" width="250">

   * etc.

#### Spawn Battle Effects

[SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs) may be used to add a visual indicator onto the [BattleUI](../../UI/Combat/BattleUI.prefab)'s [battle stage](../../UI/README.md#battle-stage) when a battle action is triggered.  When this effect is called, a visual effect may be spawned on each of the battle action's targets, across their rows, or over the entire battle stage (see `Placement` below).  

These effects are stored separately in [EffectStrategiesSpawnedArtwork](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/), and are configured with:
* `Effect Material`:  the shader material to draw - normally the shared [BattleEffect](../../VFX/Shaders/BattleEffectsEntities/BattleEffect.mat) material
* `Effect Recipe`:  a [recipe](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/BattleEffectRecipes/) asset - the shapes to draw, when, where and how they animate (e.g. three ring pops, a quarter-second apart)
  * see [Battle Effects](../../VFX/Shaders/README.md#recipes) for recipes, items, shapes and the preview window
* `Effect Tint`:  the colour over the whole effect (solid, or a linear / wave / radial gradient), so one recipe can serve several colour variants (e.g. [SpawnMultiPopBlue](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnMultiPopBlue.asset) and [SpawnMultiPopYellow](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnMultiPopYellow.asset))
* `Placement`:  where the effect is drawn
  * `Slide`:  over each target's slide (aspect fit) ~ default
  * `Row`:  across each target's row, spanning its enemies (stretched)
  * `FullScreen`:  over the whole battle stage (stretched)
* `Delay After Seconds`:  how long the battle action waits before moving on to its next effect
* `Destroy After Seconds`:  the effect's lifetime (zero to keep it until the end of combat)

For example, the effect [SpawnCirclePopPink](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnCirclePopPink.asset) will spawn a pink circle pop as shown below when Tilly uses the `Do Actual Work` skill:

![](../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectSpawnPrefabImplemented.gif)

## Battle Action Editor

The [Battle Action Editor](../../../Scripts/Combat/Editor/BattleAction/BattleActionWindow/BattleActionWindow.cs) edits a skill or action item together with its battle action + associated targeting / effect strategies in one window.

Open the editor by double-clicking a Skill or Action Item asset file (or via `Window` -> `Battle Action Editor`).  While open, it follows the selection:  selecting another skill / action item displays the corresponding skill / action item.

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/BattleActionEditor.png" width="900">

The window is split into three columns:

### A. Skill / Action Item + Battle Action

* the skill / action item's key fields (`Skill Stat` or `Consumable`, and `Battle Action`)
    * other fields (e.g. localized name / detail) must still be edited in the standard inspector
* `New Battle Action`:  makes a battle action named after the skill / action item in `Skills/` or `ActionItems/` and assigns it
* the battle action's fields (damage type, cooldown, AP cost), with a `Ping` button to find the asset
* `Targeting`:  the targeting strategy + its settings (combat participant type, filter strategies) ~ flagged red if unassigned
* `Effect Strategies`:  the battle action's effect strategy slots

Right-click the battle action's name or the `Targeting` / `Effect Strategies` headers to copy / paste between battle actions.

### B. Selected Effect Strategies

The settings of each effect strategy selected in column A's slots, in slot order.  Double-click an effect strategy's name to rename it (renames the asset file in kind).

Note that effect strategies are shared assets:  editing one here changes it for every battle action that uses it.

### C. Effect Strategy Browser

Every effect strategy in the project, grouped by what it does, and sorted within each group w/ custom filters/search functionality to narrow the effects:
* select an entry to view / edit it in the panel at the top (double-click its name to rename it)
* `Add to Battle Action`:  appends the selected effect strategy to column A's battle action
* `Make New Effect Strategy`:  creates a new effect strategy of the chosen type (named `New <Type>`, with `(1)`, `(2)`, etc. for duplicates) in that type's [EffectStrategies](../../Combat/BattleActions/EffectStrategies/) sub-folder

### Battle Action Organization

Each skill / action item has its own battle action, named after it, in `Skills/` or `ActionItems/` respectively.  `MiscUtility/` holds the few battle actions that are deliberately shared (e.g. `DoNothing`).

Note that:
* battle actions are looked up by name at runtime, so names should be unique
* deleting a skill / action item also deletes its battle action (the one in its folder with its name), unless another skill / action item still uses it
