namespace LowDefMustard.Rendering
{
    [BattleEffectMirroredEnum]
    public enum BattleEffectRepetition
    {
        None = 0,
        Line = 1, // Count copies along the spacing vector
        Cross = 2, // A line along the spacing vector + one across it (e.g. forms a plus of 5 w/ count 3)
        Grid = 3 // Count x by count y copies, spacing x / y apart
    }
}
