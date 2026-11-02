namespace LowDefMustard.Rendering
{
    [BattleEffectMirroredEnum]
    public enum BattleEffectTilingMode
    {
        None = 0,
        Grid = 1,
        StaggeredGrid = 2,
        StaggeredGridOverlapping = 3  // Each point takes its nearest copy across two rows (a shape reaching past its row stays whole) - not a true overlap
    }
}
