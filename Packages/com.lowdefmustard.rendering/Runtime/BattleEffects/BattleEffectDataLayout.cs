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
        public const int easingParameters = 6;      // (approach ratio, -, -, -)

        // Transform
        public const int position = 7;              // (start.xy, end.xy)
        public const int scale = 8;                 // (start.xy, end.xy)
        public const int elongation = 9;            // (start.xy, end.xy)
        public const int rotation = 10;              // (item rotation start, end, shape rotation start, end) [radians]

        // Style
        public const int style = 11;                // (style, blend, outline thickness start, end)
        public const int colour = 12;               // Linear rgba (palette entry A)
        public const int outlineColour = 13;        // Linear rgba (fill + outline style's stroke)

        // Alt colours (palette entries after the colour)
        public const int altColourB = 14;           // Linear rgba
        public const int altColourC = 15;
        public const int altColourD = 16;

        // Outline edge gradient
        public const int edgeGradient = 17;         // (on [0 / 1], steps [0 = smooth], -, -)
        public const int edgeColour = 18;           // Linear rgba (the stroke's outside edge)

        // Stripes
        public const int stripes = 19;              // (stripes per EU [0 = off], angle [radians], duty, phase)

        // Clip
        public const int clip = 20;                 // (clip shape, angle [radians], -, -)
        public const int clipSize = 21;             // (half size start.xy, end.xy)
        public const int clipOffset = 22;           // (centre offset start.xy, end.xy)

        // Repetition
        public const int repetition = 23;           // (repetition, count x, count y, mirror [x = 1, y = 2, beat offsets after the fold = 4])
        public const int spacing = 24;              // (start.xy, end.xy)
        public const int copyScale = 25;            // (copy scale ratio start, end, -, -)
        public const int copyWave = 26;             // (waveform, copies per cycle, amplitude start, end) ~ line / cross copies offset across their line
        public const int copyWavePhase = 27;        // (phase start, end, plateau start, end)

        // Colour cycle
        public const int colourCycle = 28;          // (source, rate, phase offset [steps], palette count [colour + alt colours in use])
        public const int colourPattern = 29;        // Pattern source:  (steps 0-11, 12-23, 24-35 [base-4 digits, step 0 = lowest], step count)

        // Beats
        public const int beatStarts = 30;           // Beats 0-3 start (item seconds;  unused = never)
        public const int beatColours = 31;          // Beats 0-3 palette index (added to the colour cycle's)
        public const int beatOffsets01 = 32;        // Beat 0 position offset.xy, beat 1 .xy
        public const int beatOffsets23 = 33;        // Beats 2 + 3
        public const int beatElongations01 = 34;    // Beat 0 elongation.xy, beat 1 .xy
        public const int beatElongations23 = 35;    // Beats 2 + 3

        // Jitter
        public const int jitter = 36;               // (rate, amount.x, amount.y, seed)
        public const int jitterMode = 37;           // (mode, phase, -, -)

        // Flicker
        public const int flicker = 38;              // (rate, depth, phase, pattern [0 = off;  bits + a leading 1])

        // Pulse
        public const int pulse = 39;                // (rate, amount, phase, -)

        // Sizes
        public const int texelsPerItem = 40;
        public const int shapeParameterCapacity = 8;
        public const int maxAltColours = 3;         // Alt colours B-D
        public const int maxBeats = 4;
    }
}
