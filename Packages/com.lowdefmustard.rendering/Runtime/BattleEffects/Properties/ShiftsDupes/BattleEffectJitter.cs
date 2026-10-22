namespace LowDefMustard.Rendering
{
    public enum BattleEffectJitter
    {
        // Note:  Values are the jitter modes of Jitter (TimeUtility.hlsl)
        Random = 0, // Re-rolled each step
        Orbit = 1 // Steps clockwise around the corners of the +/- amount box
    }
}
