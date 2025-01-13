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

### Targeting Strategy

Targeting strategies can be found in [Combat/BattleActions/TargetingStrategies](../../Combat/BattleActions/TargetingStrategies/).  To make a new targeting strategy:

1. Navigate to [Combat/BattleActions/TargetingStrategies](../../Combat/BattleActions/TargetingStrategies/)
   * depending on the complexity of the targeting strategy, navigate further to [SpecificTargeting](../../Combat/BattleActions/TargetingStrategies/SpecificTargeting/)
2. Right click and select `Create` -> `BattleAction` -> `Targeting` -> `Single Target` (or) `Multi Target`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/BattleActions/NewTargetingMenu.png" width="550">

The targeting strategy sets the rules on which target a skill **can** select, but the actual skill and target selection are done in combat via [SkillHandler](../../../Scripts/Combat/Skills/SkillHandler.cs) (for the PCs) or via the [BattleAI](../../../Scripts/Combat/BattleAI/BattleAI.cs) for the NPCs. 

#### Basic Targeting

Targeting strategies all derive from the [TargetingStrategy](../../../Scripts/Combat/BattleAction/Targeting/TargetingStrategy.cs) abstract class, and include:
* a parameter to select combat participant type: friendly, foe, or both
* filter strategies to further narrow down the target

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

#### Filtering Strategies



#### Complex Filters

### Effect Strategy


