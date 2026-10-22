using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectItem
    {
        // Notes:
        //  - Ranges run start -> end over the (eased) duration, held on progress steps if set (via retro -> held-pose animation)
        //  - Setting names carry their unit as a suffix (see BattleEffectShape)
        //  - Effect units:  0-1 spans the effect's shorter side, centred (v upwards)
        //      - the longer side reaches beyond 0-1 (e.g. a 280x320 slide spans y = -0.07 to 1.07) ~ a unit shape's size = its scale
        //  - Outline thickness may end below zero to fade the outline out through the recipe's glow
        //  - Palette:  the colour (A) + alt colours B-D can be stepped through by either the colour cycle or beats
        //  - Flicker, jitter + pulse run on the effect's clock (or:  not the item's ~ needed to ensure everything in-sync)
        //  - Mirroring + repetition happen in the shape's own space (after the transform)
        //      - this means the pattern scales / rotates w/ the entire item

        [SerializeField][Tooltip("Inspector / preview name only")] private string label = "";
        [SerializeReference] private BattleEffectShape shape = new BattleEffectCircle();

        [Header("Timing")]
        [SerializeField][Tooltip("After the effect spawns")][Min(0f)] private float startTimeSeconds = 0f;
        [SerializeField][Tooltip("The item is not drawn outside of its start + duration")][Min(0f)] private float durationSeconds = 1f;
        [SerializeField] private BattleEffectEasing easing = BattleEffectEasing.Linear;
        [SerializeField][Tooltip("Holds every range on this many evenly spaced values, each for an equal share of the duration (3 = start, middle, end; 0 = smooth)")][Min(0)] private int progressSteps = 0;

        [Header("Transform")]
        [SerializeField] private BattleEffectVector2Range positionEU = new(new Vector2(0.5f, 0.5f));
        [SerializeField] private BattleEffectVector2Range scaleEU = new(new Vector2(0.25f, 0.25f));
        [SerializeField][Tooltip("Stretches the shape's middle by +/- this along x / y (diamond -> long hexagon, circle -> capsule)")] private BattleEffectVector2Range elongationSU = new(Vector2.zero);
        [SerializeField][Tooltip("Turns the whole item about its position (incl. repetition), counter-clockwise")] private BattleEffectFloatRange itemRotationDegrees = new(0f);
        [SerializeField][Tooltip("Turns the shape within each copy, about the copy's centre, counter-clockwise")] private BattleEffectFloatRange shapeRotationDegrees = new(0f);

        [Header("Style")]
        [SerializeField] private BattleEffectStyle style = BattleEffectStyle.Outline;
        [SerializeField][Tooltip("Over:  paints over earlier items.  Erase:  punches its coverage out of them (fill and outline:  its fill only)")] private BattleEffectBlend blend = BattleEffectBlend.Over;
        [SerializeField][Tooltip("Outline + fill and outline")] private BattleEffectFloatRange outlineThicknessEU = new(0.05f);
        [SerializeField][Tooltip("Multiplied by the effect colour.  Fill / outline:  the shape.  Fill and outline:  the fill.  Palette entry A (alt colours follow)")] private Color colour = Color.white;
        [SerializeField][Tooltip("Fill and outline only:  the stroke's colour (its edge gradient blends from it).  Stripes + an erase blend apply to the fill alone")] private Color outlineColour = Color.white;
        [SerializeField] private BattleEffectAltColours altColours = new();
        [SerializeField] private BattleEffectOutlineEdgeGradient outlineEdgeGradient = new();
        [SerializeField] private BattleEffectStripes stripes = new();
        [SerializeField] private BattleEffectClip clip = new();

        [Header("Repetition")]
        [SerializeField] private BattleEffectRepetition repetition = BattleEffectRepetition.None;
        [SerializeField][Tooltip("Copies along x (line / cross:  per line;  radial:  around) - y for grid only")] private Vector2Int count = new(3, 3);
        [SerializeField][Tooltip("A unit circle spans 2. Line / cross:  the vector between copies.  Grid:  x / y gaps.  Radial:  the first copy's offset from the centre")] private BattleEffectVector2Range spacingSU = new(new Vector2(2.5f, 2.5f));
        [SerializeField][Tooltip("Folds the shape across its local y axis (left / right halves match)")] private bool mirrorX = false;
        [SerializeField][Tooltip("Folds the shape across its local x axis (top / bottom halves match)")] private bool mirrorY = false;

        [Header("Sequencing")]
        [SerializeField] private BattleEffectColourCycle colourCycle = new();
        [SerializeField][Tooltip("Held steps that move, stretch + recolour the item")] private BattleEffectBeats beats = new();

        [Header("Jitter")]
        [SerializeField][Tooltip("Position steps per second (0 = off)")][Min(0f)] private float jitterRate = 0f;
        [SerializeField][Tooltip("Random:  re-rolled each step. Orbit:  steps clockwise around the corners of the +/- amount box")] private BattleEffectJitter jitterMode = BattleEffectJitter.Random;
        [SerializeField][Tooltip("Largest offset on each axis")] private Vector2 jitterAmountEU = new(0.02f, 0.02f);
        [SerializeField][Tooltip("Random:  items w/ the same seed + rate jump together (e.g. a shape's fill + outline). Orbit:  the starting corner")] private int jitterSeed = 0;
        [SerializeField][Tooltip("Shifts the steps by this fraction of a step")][Range(0f, 1f)] private float jitterPhaseFraction = 0f;

        [Header("Flicker")]
        [SerializeField][Tooltip("Bright / dim cycles per second (0 = off)")][Min(0f)] private float flickerRate = 0f;
        [SerializeField][Tooltip("How far alpha drops on the dim half of each cycle")][Range(0f, 1f)] private float flickerDepthFraction = 0.5f;
        [SerializeField][Tooltip("Shifts the flicker by this fraction of a cycle (0.5 = dim half first)")][Range(0f, 1f)] private float flickerPhaseFraction = 0f;

        [Header("Pulse")]
        [SerializeField][Tooltip("Big / small cycles per second (0 = off)")][Min(0f)] private float pulseRate = 0f;
        [SerializeField][Tooltip("Size change on the second half of each cycle (e.g. 0.25 = 25% bigger, -0.25 = 25% smaller)")] private float pulseAmountFraction = 0.25f;
        [SerializeField][Tooltip("Shifts the pulse by this fraction of a cycle")][Range(0f, 1f)] private float pulsePhaseFraction = 0f;

        public bool IsUnset() => shape == null && durationSeconds <= 0f && colour == default; // A fresh inspector list entry is zero-filled (field initializers skipped):  no shape, no duration, clear colour
        public string GetLabel() => label;
        public float GetStartTime() => startTimeSeconds;
        public float GetEndTime() => startTimeSeconds + durationSeconds;
        public BattleEffectShape GetShape() => shape;
        public void SetShape(BattleEffectShape setShape) => shape = setShape;

        // Note:  effect scale (the recipe's) scales positions about the centre + every effect-unit size (scale, outline, jitter, beat offsets) && shape units follow
        public bool TryWriteData(Color[] texels, int rowStart, BattleEffectFloatRange[] parameterBuffer, float effectScale)
        {
            // Written in BattleEffectDataLayout's order
            if (shape == null || !shape.TryGetShapeId(out int shapeId)) { return false; }

            // Shape
            Array.Clear(parameterBuffer, 0, parameterBuffer.Length);
            shape.GetParameters(parameterBuffer);
            texels[rowStart + BattleEffectDataLayout.shape] = new Color(shapeId, 0f, 0f, 0f);
            texels[rowStart + BattleEffectDataLayout.parametersAStart] = new Color(parameterBuffer[0].GetStart(), parameterBuffer[1].GetStart(), parameterBuffer[2].GetStart(), parameterBuffer[3].GetStart());
            texels[rowStart + BattleEffectDataLayout.parametersAEnd] = new Color(parameterBuffer[0].GetEnd(), parameterBuffer[1].GetEnd(), parameterBuffer[2].GetEnd(), parameterBuffer[3].GetEnd());
            texels[rowStart + BattleEffectDataLayout.parametersBStart] = new Color(parameterBuffer[4].GetStart(), parameterBuffer[5].GetStart(), parameterBuffer[6].GetStart(), parameterBuffer[7].GetStart());
            texels[rowStart + BattleEffectDataLayout.parametersBEnd] = new Color(parameterBuffer[4].GetEnd(), parameterBuffer[5].GetEnd(), parameterBuffer[6].GetEnd(), parameterBuffer[7].GetEnd());

            // Timing
            texels[rowStart + BattleEffectDataLayout.timing] = new Color(startTimeSeconds, durationSeconds, (int)easing, progressSteps);

            // Transform
            Vector2 centre = new(0.5f, 0.5f);
            texels[rowStart + BattleEffectDataLayout.position] = ToTexel(centre + (positionEU.GetStart() - centre) * effectScale, centre + (positionEU.GetEnd() - centre) * effectScale);
            texels[rowStart + BattleEffectDataLayout.scale] = ToTexel(scaleEU.GetStart() * effectScale, scaleEU.GetEnd() * effectScale);
            texels[rowStart + BattleEffectDataLayout.elongation] = ToTexel(elongationSU);
            texels[rowStart + BattleEffectDataLayout.rotation] = new Color(itemRotationDegrees.GetStart() * Mathf.Deg2Rad, itemRotationDegrees.GetEnd() * Mathf.Deg2Rad,
                shapeRotationDegrees.GetStart() * Mathf.Deg2Rad, shapeRotationDegrees.GetEnd() * Mathf.Deg2Rad);

            // Style (+ its settings groups)
            texels[rowStart + BattleEffectDataLayout.style] = new Color((int)style, (int)blend, outlineThicknessEU.GetStart() * effectScale, outlineThicknessEU.GetEnd() * effectScale);
            texels[rowStart + BattleEffectDataLayout.colour] = colour.linear;
            texels[rowStart + BattleEffectDataLayout.outlineColour] = outlineColour.linear;
            bool isPaletteUsed = (colourCycle != null && colourCycle.IsPaletteUsed()) || (beats != null && beats.IsPaletteUsed());
            int paletteCount = altColours?.WriteData(texels, rowStart, isPaletteUsed) ?? 1;
            outlineEdgeGradient?.WriteData(texels, rowStart);
            stripes?.WriteData(texels, rowStart, effectScale);
            clip?.WriteData(texels, rowStart);

            // Repetition
            texels[rowStart + BattleEffectDataLayout.repetition] = new Color((int)repetition, Mathf.Max(count.x, 1), Mathf.Max(count.y, 1), (mirrorX ? 1f : 0f) + (mirrorY ? 2f : 0f));
            texels[rowStart + BattleEffectDataLayout.spacing] = ToTexel(spacingSU);

            // Sequencing
            if (colourCycle != null) { colourCycle.WriteData(texels, rowStart, paletteCount); }
            else { texels[rowStart + BattleEffectDataLayout.colourCycle] = new Color(0f, 0f, 0f, paletteCount); }
            if (beats != null) { beats.WriteData(texels, rowStart, effectScale); }
            else { BattleEffectBeats.WriteEmpty(texels, rowStart); }

            // Jitter, flicker + pulse
            texels[rowStart + BattleEffectDataLayout.jitter] = new Color(jitterRate, jitterAmountEU.x * effectScale, jitterAmountEU.y * effectScale, jitterSeed);
            texels[rowStart + BattleEffectDataLayout.jitterMode] = new Color((int)jitterMode, jitterPhaseFraction, 0f, 0f);
            texels[rowStart + BattleEffectDataLayout.flicker] = new Color(flickerRate, flickerDepthFraction, flickerPhaseFraction, 0f);
            texels[rowStart + BattleEffectDataLayout.pulse] = new Color(pulseRate, pulseAmountFraction, pulsePhaseFraction, 0f);
            return true;
        }

        private static Color ToTexel(BattleEffectVector2Range range) => ToTexel(range.GetStart(), range.GetEnd());
        private static Color ToTexel(Vector2 start, Vector2 end) => new(start.x, start.y, end.x, end.y);
    }
}
