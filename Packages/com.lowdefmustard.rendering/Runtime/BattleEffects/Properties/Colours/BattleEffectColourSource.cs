namespace LowDefMustard.Rendering
{
    [BattleEffectMirroredEnum]
    public enum BattleEffectColourSource
    {
        Time = 0, // Steps through the palette at the cycle's rate (on the effect's clock)
        Copy = 1 // Each repetition copy takes the next palette entry (line / radial:  along;  grid:  column + row)
    }
}
