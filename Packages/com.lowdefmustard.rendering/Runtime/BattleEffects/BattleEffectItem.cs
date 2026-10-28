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
        //  - Palette:  the colour (A) + alt colours B-D can be stepped through by either the colour cycle or beats
        //  - Settings groups (style, alt colours, ..., pulse) are classes w/ their own WriteData + drawer

        [SerializeField][Tooltip("Inspector / preview name only")] private string label = "";
        [SerializeReference] private BattleEffectShape shape = new BattleEffectCircle();

        [SerializeField][Tooltip("After the effect spawns")][Min(0f)] private float startTimeSeconds = 0f;
        [SerializeField][Tooltip("The item is not drawn outside of its start + duration")][Min(0f)] private float durationSeconds = 1f;
        [SerializeField] private BattleEffectEasing easing = BattleEffectEasing.Linear;
        [SerializeField][Tooltip("Approach only:  how much bigger it ends than it starts, as if rushing at the camera (1 = linear; 10 = from 10x as far)")][Min(1f)] private float approachRatio = 4f;
        [SerializeField][Tooltip("Holds every range on this many evenly spaced values, each for an equal share of the duration (3 = start, middle, end; 0 = smooth)")][Min(0)] private int progressSteps = 0;

        [SerializeField] private BattleEffectVector2Range positionEU = new(new Vector2(0.5f, 0.5f));
        [SerializeField] private BattleEffectVector2Range scaleEU = new(new Vector2(0.25f, 0.25f));
        [SerializeField][Tooltip("Stretches the shape's middle by +/- this along x / y (diamond -> long hexagon, circle -> capsule)")] private BattleEffectVector2Range elongationSU = new(Vector2.zero);
        [SerializeField][Tooltip("Turns the whole item about its position (incl. repetition), counter-clockwise")] private BattleEffectFloatRange itemRotationDegrees = new(0f);
        [SerializeField][Tooltip("Turns the shape within each copy, about the copy's centre, counter-clockwise")] private BattleEffectFloatRange shapeRotationDegrees = new(0f);

        [SerializeField] private BattleEffectStyle style = new();
        [SerializeField] private BattleEffectAltColours altColours = new();
        [SerializeField] private BattleEffectOutlineEdgeGradient outlineEdgeGradient = new();
        [SerializeField] private BattleEffectStripes stripes = new();
        [SerializeField] private BattleEffectClip clip = new();

        [SerializeField] private BattleEffectRepetition repetition = new();

        [SerializeField] private BattleEffectColourCycle colourCycle = new();
        [SerializeField][Tooltip("Held steps that move, stretch + recolour the item")] private BattleEffectBeats beats = new();

        [SerializeField] private BattleEffectJitter jitter = new();
        [SerializeField] private BattleEffectFlicker flicker = new();
        [SerializeField] private BattleEffectPulse pulse = new();

        // Editor Field References
        public const string labelField = nameof(label);
        public const string shapeField = nameof(shape);
        public const string startTimeSecondsField = nameof(startTimeSeconds);
        public const string durationSecondsField = nameof(durationSeconds);
        public const string easingField = nameof(easing);
        public const string approachRatioField = nameof(approachRatio);
        public const string progressStepsField = nameof(progressSteps);
        public const string positionEUField = nameof(positionEU);
        public const string scaleEUField = nameof(scaleEU);
        public const string elongationSUField = nameof(elongationSU);
        public const string itemRotationDegreesField = nameof(itemRotationDegrees);
        public const string shapeRotationDegreesField = nameof(shapeRotationDegrees);
        public const string styleField = nameof(style);
        public const string altColoursField = nameof(altColours);
        public const string outlineEdgeGradientField = nameof(outlineEdgeGradient);
        public const string stripesField = nameof(stripes);
        public const string clipField = nameof(clip);
        public const string repetitionField = nameof(repetition);
        public const string colourCycleField = nameof(colourCycle);
        public const string beatsField = nameof(beats);
        public const string jitterField = nameof(jitter);
        public const string flickerField = nameof(flicker);
        public const string pulseField = nameof(pulse);

        // Constructors
        public BattleEffectItem() { }
        internal BattleEffectItem(float startTimeSeconds) => this.startTimeSeconds = Mathf.Max(startTimeSeconds, 0f);

        public bool IsUnset() => shape == null && durationSeconds <= 0f && (style == null || style.GetColour() == default); // A fresh inspector list entry is zero-filled (i.e. field initializers skipped)
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
            texels[rowStart + BattleEffectDataLayout.easingParameters] = new Color(Mathf.Max(approachRatio, 1f), 0f, 0f, 0f);

            // Transform
            Vector2 centre = new(0.5f, 0.5f);
            texels[rowStart + BattleEffectDataLayout.position] = ToTexel(centre + (positionEU.GetStart() - centre) * effectScale, centre + (positionEU.GetEnd() - centre) * effectScale);
            texels[rowStart + BattleEffectDataLayout.scale] = ToTexel(scaleEU.GetStart() * effectScale, scaleEU.GetEnd() * effectScale);
            texels[rowStart + BattleEffectDataLayout.elongation] = ToTexel(elongationSU);
            texels[rowStart + BattleEffectDataLayout.rotation] = new Color(itemRotationDegrees.GetStart() * Mathf.Deg2Rad, itemRotationDegrees.GetEnd() * Mathf.Deg2Rad,
                shapeRotationDegrees.GetStart() * Mathf.Deg2Rad, shapeRotationDegrees.GetEnd() * Mathf.Deg2Rad);

            // Style (+ its settings groups)
            style?.WriteData(texels, rowStart, effectScale);
            bool isPaletteUsed = (colourCycle != null && colourCycle.IsPaletteUsed()) || (beats != null && beats.IsPaletteUsed()) || (style != null && style.IsPaletteUsed());
            int paletteCount = altColours?.WriteData(texels, rowStart, isPaletteUsed) ?? 1;
            outlineEdgeGradient?.WriteData(texels, rowStart);
            stripes?.WriteData(texels, rowStart, effectScale);
            clip?.WriteData(texels, rowStart);

            // Repetition
            repetition?.WriteData(texels, rowStart);

            // Sequencing
            if (colourCycle != null) { colourCycle.WriteData(texels, rowStart, paletteCount); }
            else { texels[rowStart + BattleEffectDataLayout.colourCycle] = new Color(0f, 0f, 0f, paletteCount); }
            if (beats != null) { beats.WriteData(texels, rowStart, effectScale); }
            else { BattleEffectBeats.WriteEmpty(texels, rowStart); }

            // Jitter, flicker + pulse
            jitter?.WriteData(texels, rowStart, effectScale);
            flicker?.WriteData(texels, rowStart);
            pulse?.WriteData(texels, rowStart);
            return true;
        }

        private static Color ToTexel(BattleEffectVector2Range range) => ToTexel(range.GetStart(), range.GetEnd());
        private static Color ToTexel(Vector2 start, Vector2 end) => new(start.x, start.y, end.x, end.y);
    }
}
