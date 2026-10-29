using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectShade
    {
        // Notes:
        //  - Splits each copy through its centre: a. in the copy's own space (turns w/ shape rotation) && b. mirrored halves keeping the item's side (not folded)
        //  - Offset moves the line along the shaded direction

        [SerializeField] private bool isEnabled = false;
        [SerializeField][Tooltip("Direction of the shaded side from the copy's centre (0 = right, 90 = up)")] private BattleEffectFloatRange angleDegrees = new(0f);
        [SerializeField][Tooltip("Moves the line from the copy's centre along that direction (shape units;  + = toward the shaded side)")] private BattleEffectFloatRange offsetSU = new(0f);
        [SerializeField][Tooltip("Antialiases the line (off = a hard, pixel step)")] private bool isSmooth = false;
        [SerializeField][Tooltip("Palette steps added to the fill on the shaded side")] private int fillPaletteStep = 0;
        [SerializeField][Tooltip("Palette steps added to the stroke on the shaded side")] private int outlinePaletteStep = 0;

        // Editor Field References
        public const string isEnabledField = nameof(isEnabled);
        public const string angleDegreesField = nameof(angleDegrees);
        public const string offsetSUField = nameof(offsetSU);
        public const string isSmoothField = nameof(isSmooth);
        public const string fillPaletteStepField = nameof(fillPaletteStep);
        public const string outlinePaletteStepField = nameof(outlinePaletteStep);

        internal bool IsPaletteUsed() => isEnabled && (fillPaletteStep != 0 || outlinePaletteStep != 0);

        internal void WriteData(Color[] texels, int rowStart)
        {
            if (!isEnabled)
            {
                texels[rowStart + BattleEffectDataLayout.shade] = Color.clear;
                texels[rowStart + BattleEffectDataLayout.shadeLine] = Color.clear;
                return;
            }
            texels[rowStart + BattleEffectDataLayout.shade] = new Color(isSmooth ? 2f : 1f, fillPaletteStep, outlinePaletteStep, 0f);
            texels[rowStart + BattleEffectDataLayout.shadeLine] = new Color(angleDegrees.GetStart() * Mathf.Deg2Rad, angleDegrees.GetEnd() * Mathf.Deg2Rad, offsetSU.GetStart(), offsetSU.GetEnd());
        }
    }
}
