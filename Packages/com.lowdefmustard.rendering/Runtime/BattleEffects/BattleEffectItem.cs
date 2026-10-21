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
        //  - Flicker, jitter + pulse run on the effect's clock (or:  not the item's ~ needed to ensure everything in-sync)
        //  - Mirroring + repetition happen in the shape's own space (after the transform)
        //      - this means the pattern scales / rotates w/ the entire item

        [SerializeField][Tooltip("Inspector / preview name only")] private string label = "";
        [SerializeReference] private BattleEffectShape shape = new BattleEffectCircle();

        [Header("Timing")]
        [SerializeField][Tooltip("After the effect spawns")][Min(0f)] private float startTimeSeconds = 0f;
        [SerializeField][Tooltip("The item is not drawn outside of its start + duration")][Min(0f)] private float durationSeconds = 1f;
        [SerializeField] private BattleEffectEasing easing = BattleEffectEasing.Linear;
        [SerializeField][Tooltip("Holds every range on this many evenly spaced values, each for an equal share of the duration (e.g. 3 = start, middle, end). 0 = smooth")][Min(0)] private int progressSteps = 0;

        [Header("Transform")]
        [SerializeField] private BattleEffectVector2Range positionEU = new(new Vector2(0.5f, 0.5f));
        [SerializeField] private BattleEffectVector2Range scaleEU = new(new Vector2(0.25f, 0.25f));
        [SerializeField][Tooltip("Counter-clockwise")] private BattleEffectFloatRange rotationDegrees = new(0f);
        [SerializeField][Tooltip("Stretches the shape's middle by +/- this along x / y (e.g. diamond -> long hexagon, circle -> capsule)")] private BattleEffectVector2Range elongationSU = new(Vector2.zero);

        [Header("Style")]
        [SerializeField] private BattleEffectStyle style = BattleEffectStyle.Outline;
        [SerializeField][Tooltip("Outline only")] private BattleEffectFloatRange outlineThicknessEU = new(0.05f);
        [SerializeField][Tooltip("Multiplied by the effect colour")] private Color colour = Color.white;
        [SerializeField] private BattleEffectColourCycle colourCycle = new();
        [SerializeField] private BattleEffectStripes stripes = new();

        [Header("Repetition")]
        [SerializeField] private BattleEffectRepetition repetition = BattleEffectRepetition.None;
        [SerializeField][Tooltip("Copies along x (line / cross:  per line;  radial:  around) - y for grid only")] private Vector2Int count = new(3, 3);
        [SerializeField][Tooltip("A unit circle spans 2. Line / cross:  the vector between copies.  Grid:  x / y gaps.  Radial:  the first copy's offset from the centre")] private BattleEffectVector2Range spacingSU = new(new Vector2(2.5f, 2.5f));
        [SerializeField][Tooltip("Folds the shape across its local y axis (left / right halves match)")] private bool mirrorX = false;
        [SerializeField][Tooltip("Folds the shape across its local x axis (top / bottom halves match)")] private bool mirrorY = false;

        [Header("Flicker")]
        [SerializeField][Tooltip("Bright / dim cycles per second (0 = off)")][Min(0f)] private float flickerRate = 0f;
        [SerializeField][Tooltip("How far alpha drops on the dim half of each cycle")][Range(0f, 1f)] private float flickerDepthFraction = 0.5f;
        [SerializeField][Tooltip("Shifts the flicker by this fraction of a cycle (0.5 = dim half first)")][Range(0f, 1f)] private float flickerPhaseFraction = 0f;

        [Header("Jitter")]
        [SerializeField][Tooltip("Position steps per second (0 = off)")][Min(0f)] private float jitterRate = 0f;
        [SerializeField][Tooltip("Random:  re-rolled each step. Orbit:  steps clockwise around the corners of the +/- amount box")] private BattleEffectJitter jitterMode = BattleEffectJitter.Random;
        [SerializeField][Tooltip("Largest offset on each axis")] private Vector2 jitterAmountEU = new(0.02f, 0.02f);
        [SerializeField][Tooltip("Random:  items w/ the same seed + rate jump together (e.g. a shape's fill + outline). Orbit:  the starting corner")] private int jitterSeed = 0;
        [SerializeField][Tooltip("Shifts the steps by this fraction of a step")][Range(0f, 1f)] private float jitterPhaseFraction = 0f;

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

        // Note:  effect scale (the recipe's) scales positions about the centre + every effect-unit size (scale, outline, jitter) && shape units follow
        public bool TryWriteData(Color[] texels, int rowStart, BattleEffectFloatRange[] parameterBuffer, float effectScale)
        {
            if (shape == null || !shape.TryGetShapeId(out int shapeId)) { return false; }

            Array.Clear(parameterBuffer, 0, parameterBuffer.Length);
            shape.GetParameters(parameterBuffer);

            texels[rowStart + BattleEffectDataLayout.header] = new Color(shapeId, startTimeSeconds, durationSeconds, (int)easing);
            Vector2 centre = new(0.5f, 0.5f);
            texels[rowStart + BattleEffectDataLayout.position] = ToTexel(centre + (positionEU.GetStart() - centre) * effectScale, centre + (positionEU.GetEnd() - centre) * effectScale);
            texels[rowStart + BattleEffectDataLayout.scale] = ToTexel(scaleEU.GetStart() * effectScale, scaleEU.GetEnd() * effectScale);
            texels[rowStart + BattleEffectDataLayout.rotationOutline] = new Color(rotationDegrees.GetStart() * Mathf.Deg2Rad, rotationDegrees.GetEnd() * Mathf.Deg2Rad, outlineThicknessEU.GetStart() * effectScale, outlineThicknessEU.GetEnd() * effectScale);
            texels[rowStart + BattleEffectDataLayout.style] = new Color((int)style, (int)jitterMode, jitterPhaseFraction, progressSteps);
            texels[rowStart + BattleEffectDataLayout.colour] = colour.linear;
            texels[rowStart + BattleEffectDataLayout.parametersAStart] = new Color(parameterBuffer[0].GetStart(), parameterBuffer[1].GetStart(), parameterBuffer[2].GetStart(), parameterBuffer[3].GetStart());
            texels[rowStart + BattleEffectDataLayout.parametersAEnd] = new Color(parameterBuffer[0].GetEnd(), parameterBuffer[1].GetEnd(), parameterBuffer[2].GetEnd(), parameterBuffer[3].GetEnd());
            texels[rowStart + BattleEffectDataLayout.parametersBStart] = new Color(parameterBuffer[4].GetStart(), parameterBuffer[5].GetStart(), parameterBuffer[6].GetStart(), parameterBuffer[7].GetStart());
            texels[rowStart + BattleEffectDataLayout.parametersBEnd] = new Color(parameterBuffer[4].GetEnd(), parameterBuffer[5].GetEnd(), parameterBuffer[6].GetEnd(), parameterBuffer[7].GetEnd());
            texels[rowStart + BattleEffectDataLayout.flicker] = new Color(flickerRate, flickerDepthFraction, flickerPhaseFraction, 0f);
            texels[rowStart + BattleEffectDataLayout.repetition] = new Color((int)repetition, Mathf.Max(count.x, 1), Mathf.Max(count.y, 1), (mirrorX ? 1f : 0f) + (mirrorY ? 2f : 0f));
            texels[rowStart + BattleEffectDataLayout.spacing] = ToTexel(spacingSU);
            texels[rowStart + BattleEffectDataLayout.elongation] = ToTexel(elongationSU);
            texels[rowStart + BattleEffectDataLayout.pulse] = new Color(pulseRate, pulseAmountFraction, pulsePhaseFraction, 0f);
            texels[rowStart + BattleEffectDataLayout.jitter] = new Color(jitterRate, jitterAmountEU.x * effectScale, jitterAmountEU.y * effectScale, jitterSeed);
            colourCycle?.WriteData(texels, rowStart);
            stripes?.WriteData(texels, rowStart, effectScale);
            return true;
        }

        private static Color ToTexel(BattleEffectVector2Range range) => ToTexel(range.GetStart(), range.GetEnd());
        private static Color ToTexel(Vector2 start, Vector2 end) => new(start.x, start.y, end.x, end.y);
    }
}
