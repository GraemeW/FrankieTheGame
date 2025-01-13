# Assets:  Game - Battle Actions

Battle actions may be taken by characters in Frankie to deal damage, heal, apply status effects, call for help, etc. They are used by both [Skills](../Skills/) and [Action Items](../Inventory/ActionItems/). See, for example, the battle action for single target damage below:

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleActionExample.png" width="350">

Asset File:  **[SingleTargetDamageHeavyCrit](./SkillBase/SingleTargetDamageSmallHeavyCrit.asset)**


A high-level summary on Battle Actions and their overall role in the combat system is described in [Game/Combat](../../Combat/).

## Battle Actions: Quick Start Guide

### Make the Battle Action

1. Navigate to either:
* [SkillBase](./SkillBase/) directory for battle actions intended for skills
* [ItemBase](./ItemBase/) directory for battle actions intended for items

*N.B.  Strictly speaking, battle actions don't care if they're on items or skills, but this just helps for organization*

2. Right click and select `Create` -> `BattleAction` -> `New Battle Action`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/NewBattleActionMenu.png" width="450">

### Base Parameters

Set the `Other Input` parameters:
* Damage Type: either `Physical` or `Magical`, which notably selects for the defense stats opposed to the skill
* Cooldown: in seconds
* AP Cost: optional (set to 0 if no cost)

The [Targeting Strategy](#targeting-strategy) and [Effect Strategies](#effect-strategy) are described in more detail below.

**Critical Note:**  As discussed below in [Trigger Resources & Cooldowns](#trigger-resources--cooldowns), it is crucial to have the final effect on any battle action as the [TriggerResourcesCooldowns](../../Combat/BattleActions/EffectStrategies/TriggerResourcesCooldowns.asset.meta) effect strategy**

### Targeting Strategy

To make a new targeting strategy:

1. Navigate to [Combat/BattleActions/TargetingStrategies](../../Combat/BattleActions/TargetingStrategies/)
   * depending on the complexity of the targeting strategy, navigate further to [SpecificTargeting](../../Combat/BattleActions/TargetingStrategies/SpecificTargeting/)
2. Right click and select `Create` -> `BattleAction` -> `Targeting` -> `Single Target` (or) `Multi Target`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/NewTargetingMenu.png" width="550">

The targeting strategy sets the rules on which target a skill **can** select, but the actual skill and target selection are done in combat via [SkillHandler](../../../Scripts/Combat/Skills/SkillHandler.cs) (for the PCs) or via the [BattleAI](../../../Scripts/Combat/BattleAI/BattleAI.cs) for the NPCs. 

#### Basic Targeting

Targeting strategies all derive from the [TargetingStrategy](../../../Scripts/Combat/BattleAction/Targeting/TargetingStrategy.cs) abstract class, and include:
* a parameter to select combat participant type: friendly, foe, or both
* [filter strategies](#filtering-strategy) to further narrow down the target

Friendly/foe description are relative to the character's disposition to the player.  The characters in a player's party are friendly to each other, and see enemies in combat as foes.  Likewise, an enemy is friendly to other enemies in combat, and sees the player characters as foes.

In the above example **[SingleTargetDamageHeavyCrit](./SkillBase/SingleTargetDamageSmallHeavyCrit.asset)**, the targeting strategy is [SingleTargetLivingEnemies](../../Combat/BattleActions/TargetingStrategies/SingleTargetLivingEnemies.asset), which means the skill can only target a single character in opposition to the active character's disposition.  This targeting strategy furthermore applies a `LivingFilter`, such that it can only be applied to combat participants that are alive (i.e. hit points > 0).

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/TargetingSingleEnemy.png" width="350">

#### Single vs. Multi Targeting

The above example covers the simple [Single Target](../../../Scripts/Combat/BattleAction/Targeting/SingleTargeting.cs) strategy.  However, many skills impact more than one character, and are instead handled by [MultiTargeting](../../../Scripts/Combat/BattleAction/Targeting/MultiTargeting.cs).

See for example [Trample](./SkillBase/OldStyle/Trample.asset) asset:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/BattleActionTrample.png" width="350">

, which applies a medium health deduction to all enemies, by making use of the [MultiTargetAllEnemies](../../Combat/BattleActions/TargetingStrategies/MultiTargetAllEnemies.asset) targeting strategy:

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

Effect strategies derive from the [EffectStrategy](../../../Scripts/Combat/BattleAction/Effects/EffectStrategy.cs) abstract class, which requires a method to initiate the effect and a callback for when the effect is finished.  

#### Basic Effects

Some example basic effects include:
   * health effect: deal damage or heal - such as [DeductHPMed](../../Combat/BattleActions/EffectStrategies/DirectHP/DeductHPMed.asset)
     * health is modified by Health Change ± Jitter
     * apply damage type selects if physical/magical defense should be used in damage calculations

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectDeductHP.png" width="250">

   * ap effect: add/remove action points from the character - such as [RestoreAPSmall](../../Combat/BattleActions/EffectStrategies/DirectAP/RestoreAPSmall.asset)
     * action points are modified by AP Change ± Jitter

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectRestoreAP.png" width="250">

   * persistent stat effect: increment or decrement a [stat](../../../Scripts/Stats/Stat.cs) temporarily - such as [IncreaseLuckMed](../../Combat/BattleActions/EffectStrategies/PersistentStatMods/IncreaseLuckMed.asset)
     * fraction probability sets the likelihood of effect applying (where 1 = 100%, 0 = 0%) on skill use
     * persist after combat defines whether the effect clears automatically after combat, or remains until duration expires

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectPersistentStat.png" width="250">

   * dot/hot effect: damage or heal over time - such as [DoTHPSmall](../../Combat/BattleActions/EffectStrategies/DoT/DoTHPSmall.asset)
     * health is modified by health change per teck every tick period seconds
     * fraction probability & persist match are consistent with the above explanation ^

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectDamageOverTime.png" width="250">
   
   * remove persistent stat effects: to clear the above stat/dot/hots - such as [ClearStatus_All](../../Combat/BattleActions/EffectStrategies/OtherHealing/ClearStatus_All.asset)
     * remove persistent recurring applies to HoT/DoTs
     * remove persistent stat refers to persistent stat effects
     * fraction probability defines chance of success to remove the stat
     * number of effects = 0 will remove all effects, otherwise removes number indicated

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectRemoveStatus.png" width="375">

   * permanent stat effect: increment or decrement a [stat](../../../Scripts/Stats/Stat.cs) permanently - such as [PermanentIncreaseBeautyLarge](../../Combat/BattleActions/EffectStrategies/PermanentStatMods/PermanentIncrementBeautyLarge.asset) 
   
<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectPermanentStat.png" width="250">

   * set cooldown: overrides the cooldown on the target (e.g. to delay enemy actions) - such as [SetLongCooldown](../../Combat/BattleActions/EffectStrategies/SetCooldown/SetLongCooldown.asset)

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectSetCooldown.png" width="250">

   * call for help effect: add new enemies to the current combat - such as [CallForHelpSquirrel](../../Combat/BattleActions/EffectStrategies/CallForHelp/CallForHelpSquirrel.asset)
     * , using the same logic as [EnemySpawner](../../WorldObjects/zz_Spawners/) configurations
     * , where a variable number of enemies (defined by their [Character Properties](../CharacterProperties/)) can be added with a given probability

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectCallForHelp.png" width="250">

   * etc.

#### Trigger Resources & Cooldowns

The [TriggerResourcesCooldowns](../../Combat/BattleActions/EffectStrategies/TriggerResourcesCooldowns.asset) effect strategy **must** be placed as the final effect strategy for any battle action.  As is evident from its name, this effect will trigger the character's cooldown and subtract any AP incurred by use of the skill.

For reasons that will become evident in [Delay Composites](#delay-composites), this effect strategy is needed to signal to the [BattleController](../../Controllers/Battle%20Controller.prefab) that the current action has completed.

#### Spawn Target Prefab Effects

The [SpawnTargetPrefab](../../../Scripts/Combat/BattleAction/Effects/SpawnTargetPrefabEffect.cs) effect may be used to add a visual indicator of a battle action on the [BattleCanvas](../../UI/Combat/Battle%20Canvas.prefab) UI.  When this effect is called, a game object may be spawned globally over the entire canvas or individually on the battle action's target.  These effects are stored separately in [EffectStrategiesSpawnedArtwork](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/).

For example, the effect [SpawnCirclePopPink](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnCirclePopPink.asset):

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectSpawnPrefab.png" width="325">

, will spawn the game object [CirclePopPink](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/CirclePopPink.prefab), as shown below when Tilly uses the `Do Actual Work` skill:

![](../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectSpawnPrefabImplemented.gif)

#### Action & Delayed Composites

For more complex battle actions, such as those using visual indicators (e.g. [Spawn Target Prefab Effects](#spawn-target-prefab-effects)), it is often necessary to chain together series of effects with delays.  Specifically, we want:
1. to have the visual/graphic effects to appear before the damage and status effect are applied
2. to avoid having other actions called by the [BattleController](../../Controllers/Battle%20Controller.prefab) while the current action is being executed

The [DelayComposite](../../../Scripts/Combat/BattleAction/Effects/DelayCompositeEffect.cs) addresses this complexity by allowing for a battle action to chain together an arbitrary number of sub-lists of effects preceded by pre-defined delays.  

For example, see the [DoActualWork](./SkillBase/OldStyle/DoActualWork.asset) Battle Action asset:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectCompositeBattleAction.png" width="300">

, which (in its Effect Strategies) calls:
* [SpawnCirclePopPink](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnCirclePopPink.asset)
* , followed by [DoActualWorkDelayComposite](../../Combat/BattleActions/EffectStrategies/ActionComposites/DoActualWorkDelayComposite.asset)

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/EffectCompositeEffect.png" width="300">

, such that the battle action will:
1. Spawn the game object [CirclePopPink](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/CirclePopPink.prefab)
2. Delay 0.5 seconds
3. Deduct a small amount HP via `DeductHPTouch`
4. Apply a damage over time effect via `DoTHPSmall`
5. Trigger AP cost (3) and cooldown (8 seconds)

Critically, per above note in [Trigger Resources & Cooldowns](#trigger-resources--cooldowns), the final effect in the battle action is the [TriggerResourcesCooldowns](../../Combat/BattleActions/EffectStrategies/TriggerResourcesCooldowns.asset) effect strategy.
