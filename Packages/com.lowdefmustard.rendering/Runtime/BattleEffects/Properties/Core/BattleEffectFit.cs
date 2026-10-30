namespace LowDefMustard.Rendering
{
    [BattleEffectMirroredEnum]
    public enum BattleEffectFit
    {
        AspectFit = 0, // Effect units span the shorter side, centred (shapes keep proportions)
        Stretch = 1 // Effect units span each side (shapes stretch)
    }
}
