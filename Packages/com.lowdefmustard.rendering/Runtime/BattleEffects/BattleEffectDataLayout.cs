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
        public const int outlineSource = 14;        // (from the palette [0 / 1], palette step, -, -)

        // Alt colours (palette entries after the colour)
        public const int altColourB = 15;           // Linear rgba
        public const int altColourC = 16;
        public const int altColourD = 17;

        // Outline edge gradient
        public const int edgeGradient = 18;         // (on [0 / 1], steps [0 = smooth], from the palette [0 / 1], palette step)
        public const int edgeColour = 19;           // Linear rgba (the stroke's outside edge)

        // Shade (a palette step on one side of each copy)
        public const int shade = 20;                // (mode [0 = off, 1 = hard, 2 = smooth], fill palette step, outline palette step, -)
        public const int shadeLine = 21;            // (angle start, end [radians], offset start, end [shape units, toward the shaded side])

        // Stripes
        public const int stripes = 22;              // (stripes per EU [0 = off], angle [radians], duty, phase)

        // Stipple
        public const int stipple = 23;              // (dots per EU [0 = off], density start, end, seed)

        // Clip
        public const int clip = 24;                 // (clip shape, angle [radians], clip whole shape, -)
        public const int clipSize = 25;             // (half size start.xy, end.xy)
        public const int clipOffset = 26;           // (centre offset start.xy, end.xy)

        // Repetition
        public const int repetition = 27;           // (repetition, count x, count y, mirror [x = 1, y = 2, half turn = 4, spread = 8, flip x = 16])
        public const int spacing = 28;              // (start.xy, end.xy)
        public const int copyScale = 29;            // (copy scale ratio start, end, -, -)
        public const int copyWave = 30;             // (waveform, copies per cycle, amplitude start, end) ~ line / cross copies offset across their line
        public const int copyWavePhase = 31;        // (phase start, end, plateau start, end)

        // Tiling (the whole item on a lattice)
        public const int tiling = 32;               // (mode [0 = off, 1 = grid, 2 = staggered grid, 3 = overlapping], count x, count y, is anchored)
        public const int tilingSpacing = 33;        // (spacing EU x, y, anchor EU x, y)

        // Colour cycle
        public const int colourCycle = 34;          // (source, rate, phase offset [steps], palette count [colour + alt colours in use])
        public const int colourPattern = 35;        // Pattern source:  (steps 0-11, 12-23, 24-35 [base-4 digits], step count)

        // Beats
        public const int beatStarts = 36;           // Beats 0-3 start (item seconds;  unused = never)
        public const int beatColours = 37;          // Beats 0-3 palette index (added to the colour cycle's)
        public const int beatOffsets01 = 38;        // Beat 0 position offset.xy, beat 1 .xy
        public const int beatOffsets23 = 39;        // Beats 2 + 3
        public const int beatElongations01 = 40;    // Beat 0 elongation.xy, beat 1 .xy
        public const int beatElongations23 = 41;    // Beats 2 + 3
        public const int beatScales = 42;           // Beats 0-3 scale ratio (x the item's scale;  1 = unchanged)
        public const int beatPattern = 43;          // (steps 0-9, 10-19 [base-5 digits:  0 = no beat, 1-4 = beats 0-3], rate, step count [-1 = interpolated;  0 = by start times])

        // Jitter
        public const int jitter = 44;               // (rate, amount.x, amount.y, seed)
        public const int jitterMode = 45;           // (mode, phase, -, -)

        // Flicker
        public const int flicker = 46;              // (rate, depth, phase, pattern [0 = off;  bits + a leading 1])

        // Pulse
        public const int pulse = 47;                // (rate, amount, phase, -)

        // Sizes
        public const int texelsPerItem = 48;
        public const int shapeParameterCapacity = 8;
        public const int maxAltColours = 3;         // Alt colours B-D
        public const int maxBeats = 4;
    }
}
