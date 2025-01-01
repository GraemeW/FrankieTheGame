# Assets: Game - Combat

A high-level summary of the Battle Action system in Frankie is shown below:

<img src="../../../InfoTools/Documentation/Game/Combat/Frankie-BattleActions.png" width="350">

## Battle Actions

[BattleActions](../OnLoadAssets/BattleActions/) are the backbone of both the skill and inventory systems.  They are addressables found in [OnLoadAssets](../OnLoadAssets/), and are comprised of:
  * [EffectStrategies](./BattleActions/EffectStrategies/): to adjust HP/AP, apply status effects, modify stats, override cooldown, call for help, etc.
    * [EffectStrategiesSpawnedArtwork](./BattleActions/EffectStrategiesSpawnedArtwork/) are a special category of EffectStrategies that can be used to spawn visual feedback/artwork on the [BattleCanvas](../UI/Combat/Battle%20Canvas.prefab)
  * [TargetingStrategies](./BattleActions/TargetingStrategies/): to set the active target type (single vs. multi, enemy vs. ally, etc.)
    * , where [FilterStrategies](./BattleActions/FilterStrategies/) are used to further narrow the targeting strategy - e.g. to single characters

## Skills


