namespace Frankie.Combat
{
    public enum BattleEffectPlacement
    {
        Slide = 0, // Over each recipient's slide (aspect fit)
        Row = 1, // Across each recipient's row, spanning its enemies (stretched)
        FullScreen = 2 // Over the whole battle stage (stretched)
    }
}
