namespace LowDefMustard.Rendering
{
    [BattleEffectMirroredEnum]
    public enum BattleEffectFit
    {
        AspectFit = 0, // Effect units span the shorter side, centred (shapes keep their proportions)
        Stretch = 1 // Effect units span each side (fills the element, shapes stretch w/ it)
    }
}
