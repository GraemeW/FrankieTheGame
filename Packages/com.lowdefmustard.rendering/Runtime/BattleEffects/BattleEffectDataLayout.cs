namespace LowDefMustard.Rendering
{
    public static class BattleEffectDataLayout
    {
        // Notes:
        //  - Texel layout of one item in a recipe's data texture
        //      - or:   a single item fills texelsPerItem array entries
        //      -       , with each entry corresponding to a texel ~ laid out in (parentheses)
        //  - Every public const here is mirrored into HLSL by BattleEffectDispatchGenerator (e.g. scale -> BATTLE_EFFECT_LAYOUT_SCALE)

        // Shape
        public const int shape = 0;                 // (shape ID, -, -, -)
        public const int parametersAStart = 1;      // Shape parameters 0-3
        public const int parametersAEnd = 2;
        public const int parametersBStart = 3;      // Shape parameters 4-7
        public const int parametersBEnd = 4;

        // Timing
        public const int timing = 5;                // (start time [s], duration [s], easing, progress steps)

        // Transform
        public const int position = 6;              // (start.xy, end.xy)
        public const int scale = 7;                 // (start.xy, end.xy)
        public const int elongation = 8;            // (start.xy, end.xy)
        public const int rotation = 9;              // (item rotation start, end, shape rotation start, end) [radians]

        // Style
        public const int style = 10;                // (style, blend, outline thickness start, end)
        public const int colour = 11;               // Linear rgba (palette entry A)
        public const int outlineColour = 12;        // Linear rgba (fill + outline style's stroke)

        // Alt colours (palette entries after the colour)
        public const int altColourB = 13;           // Linear rgba
        public const int altColourC = 14;
        public const int altColourD = 15;

        // Outline edge gradient
        public const int edgeGradient = 16;         // (on [0 / 1], steps [0 = smooth], -, -)
        public const int edgeColour = 17;           // Linear rgba (the stroke's outside edge)

        // Stripes
        public const int stripes = 18;              // (stripes per EU [0 = off], angle [radians], duty, phase)

        // Clip
        public const int clip = 19;                 // (clip shape, angle [radians], -, -)
        public const int clipSize = 20;             // (half size start.xy, end.xy)

        // Repetition
        public const int repetition = 21;           // (repetition, count x, count y, mirror [x = 1, y = 2])
        public const int spacing = 22;              // (start.xy, end.xy)

        // Colour cycle
        public const int colourCycle = 23;          // (source, rate, phase offset [steps], palette count [colour + alt colours in use])

        // Beats
        public const int beatStarts = 24;           // Beats 0-3 start (item seconds;  unused = never)
        public const int beatColours = 25;          // Beats 0-3 palette index (added to the colour cycle's)
        public const int beatOffsets01 = 26;        // Beat 0 position offset.xy, beat 1 .xy
        public const int beatOffsets23 = 27;        // Beats 2 + 3
        public const int beatElongations01 = 28;    // Beat 0 elongation.xy, beat 1 .xy
        public const int beatElongations23 = 29;    // Beats 2 + 3

        // Jitter
        public const int jitter = 30;               // (rate, amount.x, amount.y, seed)
        public const int jitterMode = 31;           // (mode, phase, -, -)

        // Flicker
        public const int flicker = 32;              // (rate, depth, phase, -)

        // Pulse
        public const int pulse = 33;                // (rate, amount, phase, -)

        // Sizes
        public const int texelsPerItem = 34;
        public const int shapeParameterCapacity = 8;
        public const int maxAltColours = 3;         // Alt colours B-D
        public const int maxBeats = 4;
    }
}
