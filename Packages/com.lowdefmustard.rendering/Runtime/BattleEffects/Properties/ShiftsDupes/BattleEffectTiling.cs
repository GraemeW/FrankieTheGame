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
        //  - the lattice follows the item's position unless anchored (for moving items, a moving lattice would otherwise drag its cell edges through the shape)
        //  - instances can tile every item they bring in (their own tiling replaces the items')

        [SerializeField] private BattleEffectTilingMode mode = BattleEffectTilingMode.None;
        [SerializeField][Tooltip("Cells along x / rows along y")] private Vector2Int count = new(3, 3);
        [SerializeField][Tooltip("Between cells along x / rows along y")] private Vector2 spacingEU = new(0.25f, 0.25f);
        [SerializeField][Tooltip("Fixes the lattice's centre, rather than following the item's position")] private bool isAnchored = false;
        [SerializeField][Tooltip("The lattice's centre, when anchored")] private Vector2 anchorEU = new(0.5f, 0.5f);

        // Editor Field References
        public const string modeField = nameof(mode);
        public const string countField = nameof(count);
        public const string spacingEUField = nameof(spacingEU);
        public const string isAnchoredField = nameof(isAnchored);
        public const string anchorEUField = nameof(anchorEU);

        // Constructors
        public BattleEffectTiling() { }
        public BattleEffectTiling(BattleEffectTilingMode mode, Vector2Int count, Vector2 spacingEU, bool isAnchored = false, Vector2 anchorEU = default)
        {
            this.mode = mode;
            this.count = count;
            this.spacingEU = spacingEU;
            this.isAnchored = isAnchored;
            this.anchorEU = anchorEU;
        }

        public bool IsTiled() => mode != BattleEffectTilingMode.None;

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            texels[rowStart + BattleEffectDataLayout.tiling] = new Color((int)mode, Mathf.Max(count.x, 1), Mathf.Max(count.y, 1), isAnchored ? 1f : 0f);
            texels[rowStart + BattleEffectDataLayout.tilingSpacing] = new Color(spacingEU.x * effectScale, spacingEU.y * effectScale, isAnchored ? anchorEU.x : 0f, isAnchored ? anchorEU.y : 0f);
        }
    }
}
