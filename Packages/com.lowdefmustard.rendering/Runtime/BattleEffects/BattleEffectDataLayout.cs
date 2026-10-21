namespace LowDefMustard.Rendering
{
    public static class BattleEffectDataLayout
    {
        // Notes:
        //  - Texel columns of one item (one row per item) in a recipe's data texture
        //  - Every public const here is mirrored into HLSL by BattleEffectDispatchGenerator (e.g. scale -> BATTLE_EFFECT_LAYOUT_SCALE)

        public const int header = 0;            // (shape ID, start time, duration, easing)
        public const int position = 1;          // (start.xy, end.xy)
        public const int scale = 2;             // (start.xy, end.xy)
        public const int rotationOutline = 3;   // (rotation start, rotation end [radians], outline thickness start, end)
        public const int style = 4;             // (style, jitter mode, jitter phase, progress steps)
        public const int colour = 5;            // Linear rgba
        public const int parametersAStart = 6;  // Shape parameters 0-3
        public const int parametersAEnd = 7;
        public const int parametersBStart = 8;  // Shape parameters 4-7
        public const int parametersBEnd = 9;
        public const int flicker = 10;          // (rate, depth, phase, -)
        public const int repetition = 11;       // (repetition, count x, count y, mirror [x = 1, y = 2])
        public const int spacing = 12;          // (start.xy, end.xy)
        public const int elongation = 13;       // (start.xy, end.xy)
        public const int pulse = 14;            // (rate, amount, phase, -)
        public const int jitter = 15;           // (rate, amount.x, amount.y, seed)
        public const int colourCycle = 16;      // (rate, palette count [colour + cycle colours], phase offset [steps], -)
        public const int cycleColourB = 17;     // Linear rgba (palette entries after the item's colour, which is A)
        public const int cycleColourC = 18;
        public const int cycleColourD = 19;
        public const int stripes = 20;          // (stripes per EU [0 = off], angle [radians], duty, phase)
        public const int texelsPerItem = 21;
        public const int maxCycleColours = 3;   // Cycle colours B-D
        public const int shapeParameterCapacity = 8;
    }
}
