using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectTiling
    {
        // Notes:
        //  - each point is drawn from its nearest cell only (shapes reaching past their cell are cut at its edge)
        //  - stripes stay fixed to the effect
        //  - instances can tile every item they bring in (their own tiling replaces the items')

        [SerializeField] private BattleEffectTilingMode mode = BattleEffectTilingMode.None;
        [SerializeField][Tooltip("Cells along x / rows along y")] private Vector2Int count = new(3, 3);
        [SerializeField][Tooltip("Between cells along x / rows along y")] private Vector2 spacingEU = new(0.25f, 0.25f);

        // Editor Field References
        public const string modeField = nameof(mode);
        public const string countField = nameof(count);
        public const string spacingEUField = nameof(spacingEU);

        // Constructors
        public BattleEffectTiling() { }
        public BattleEffectTiling(BattleEffectTilingMode mode, Vector2Int count, Vector2 spacingEU)
        {
            this.mode = mode;
            this.count = count;
            this.spacingEU = spacingEU;
        }

        public bool IsTiled() => mode != BattleEffectTilingMode.None;

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            texels[rowStart + BattleEffectDataLayout.tiling] = new Color((int)mode, Mathf.Max(count.x, 1), Mathf.Max(count.y, 1), 0f);
            texels[rowStart + BattleEffectDataLayout.tilingSpacing] = new Color(spacingEU.x * effectScale, spacingEU.y * effectScale, 0f, 0f);
        }
    }
}
