namespace LowDefMustard.Rendering
{
    public enum BattleEffectTintMode
    {
        // Note:  Values are the gradient modes of ColourGradient (ColourUtility.hlsl)
        Solid = 0,
        Linear = 1, // A -> B along the angle
        Wave = 2, // Bands along the angle:  A at the peaks, B in the troughs
        Radial = 3 // Rings out from the centre:  A at the peaks, B in the troughs
    }
}
