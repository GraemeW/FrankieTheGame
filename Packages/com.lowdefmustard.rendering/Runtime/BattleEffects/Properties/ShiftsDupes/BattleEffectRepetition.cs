using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectRepetition
    {
        // Notes:
        //  - Operates in the shape's own space (after the transform) ~ i.e. the pattern scales / rotates w/ the entire item
        //  - Mirrors apply w/ or w/o copies

        [SerializeField] private BattleEffectRepetitionMode mode = BattleEffectRepetitionMode.None;
        [SerializeField][Tooltip("Copies (line / cross:  per line;  radial:  around;  grids:  along x) - y:  grids' rows")] private Vector2Int count = new(3, 3);
        [SerializeField][Tooltip("A unit circle spans 2. Line / cross:  the vector between copies.  Grid:  x / y gaps.  Radial:  the first copy's offset from the centre")] private BattleEffectVector2Range spacingSU = new(new Vector2(2.5f, 2.5f));
        [SerializeField][Tooltip("Each copy's size * this, compounding along the copies (copy n = ratio ^ n, about its own centre;  1 [or <= 0] = all the same)")] private BattleEffectFloatRange copyScaleRatio = new(1f);
        [SerializeField][Tooltip("Folds the shape across its local y axis (left / right halves match)")] private bool mirrorX = false;
        [SerializeField][Tooltip("Folds the shape across its local x axis (top / bottom halves match)")] private bool mirrorY = false;

        // Editor Field References
        public const string modeField = nameof(mode);
        public const string countField = nameof(count);
        public const string spacingSUField = nameof(spacingSU);
        public const string copyScaleRatioField = nameof(copyScaleRatio);
        public const string mirrorXField = nameof(mirrorX);
        public const string mirrorYField = nameof(mirrorY);

        internal void WriteData(Color[] texels, int rowStart)
        {
            texels[rowStart + BattleEffectDataLayout.repetition] = new Color((int)mode, Mathf.Max(count.x, 1), Mathf.Max(count.y, 1), (mirrorX ? 1f : 0f) + (mirrorY ? 2f : 0f));
            texels[rowStart + BattleEffectDataLayout.spacing] = new Color(spacingSU.GetStart().x, spacingSU.GetStart().y, spacingSU.GetEnd().x, spacingSU.GetEnd().y);
            texels[rowStart + BattleEffectDataLayout.copyScale] = new Color(GetCopyScaleRatio(copyScaleRatio.GetStart()), GetCopyScaleRatio(copyScaleRatio.GetEnd()), 0f, 0f);
        }

        private static float GetCopyScaleRatio(float ratio) => ratio > 0f ? ratio : 1f; // Zero-filled (e.g. older / new list entries) = off
    }
}
