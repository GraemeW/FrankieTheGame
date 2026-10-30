using System;
using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectInstance
    {
        // Notes:
        //  - Recipe instance's items are flattened into this recipe at bake (re-timed, mirrored + recoloured accordingly)
        //      - mirrors are positioned about the effect's centre
        //      - then we apply scale ratio (about the effect's centre) + offset to place it
        //      - the instanced recipe's own instances come along (up to `maxInstanceDepth` levels deep, per BattleEffectRecipe)
        //  - Global recipe parameters (scale ratio, frame rate, edge softness + glow) derive from the original recipe holder
        //  - A negative start offset brings the items in earlier, up to t=0
        //  - Tiling (if set) replaces every instanced item's own - set using this recipe's effect units (after the placement)

        // Tunables
        [SerializeField][Tooltip("Inspector name only")] private string label = "";
        [SerializeField] private BattleEffectRecipe recipe;
        [SerializeField][Tooltip("Added to every instanced item's start (negative = earlier, up to t=0)")] private float startOffsetSeconds = 0f;
        [SerializeField][Tooltip("Left <-> right, about the effect's centre")] private bool mirrorX = false;
        [SerializeField][Tooltip("Top <-> bottom, about the effect's centre")] private bool mirrorY = false;
        [SerializeField][Tooltip("Sizes the instance about the effect's centre, per axis (after the mirror)")] private Vector2 scaleRatio = Vector2.one;
        [SerializeField][Tooltip("Moves the instance (after the scale)")] private Vector2 offsetEU = Vector2.zero;
        [SerializeField][Tooltip("Replaces item colours (colour, outline colour, alt colours + edge gradient colour)")] private List<BattleEffectColourSwap> colourSwaps = new();
        [SerializeField][Tooltip("Repeats every instanced item on a lattice (replaces their own tiling)")] private BattleEffectTiling tiling = new();

        // Editor Field References
        public const string labelField = nameof(label);
        public const string recipeField = nameof(recipe);
        public const string startOffsetSecondsField = nameof(startOffsetSeconds);
        public const string mirrorXField = nameof(mirrorX);
        public const string mirrorYField = nameof(mirrorY);
        public const string scaleRatioField = nameof(scaleRatio);
        public const string offsetEUField = nameof(offsetEU);
        public const string colourSwapsField = nameof(colourSwaps);
        public const string tilingField = nameof(tiling);

        // Constructors
        public BattleEffectInstance() { }
        public BattleEffectInstance(BattleEffectRecipe recipe, float startOffsetSeconds, 
            bool mirrorX, bool mirrorY, Vector2 scaleRatio, Vector2 offsetEU, IEnumerable<BattleEffectColourSwap> colourSwaps, BattleEffectTiling tiling = null)
        {
            this.recipe = recipe;
            this.startOffsetSeconds = startOffsetSeconds;
            this.mirrorX = mirrorX;
            this.mirrorY = mirrorY;
            this.scaleRatio = scaleRatio;
            this.offsetEU = offsetEU;
            this.colourSwaps = new List<BattleEffectColourSwap>(colourSwaps);
            this.tiling = tiling ?? new BattleEffectTiling();
        }

        #region Public Methods
        public string GetLabel() => label;
        public BattleEffectRecipe GetRecipe() => recipe;
        public float GetStartOffset() => startOffsetSeconds;
        public bool IsScaleUnset() => scaleRatio == Vector2.zero; // A fresh inspector list entry is zero-filled (i.e. field initializers skipped)
        public void ResetScale() => scaleRatio = Vector2.one;
        #endregion
        
        #region PrivateInternalMethods
        internal void ApplyTo(Color[] texels, int rowStart)
        {
            // Re-times, mirrors + recolours one baked item row
            Color timing = texels[rowStart + BattleEffectDataLayout.timing];
            timing.r += startOffsetSeconds;
            if (timing.r < 0f) { timing.g = Mathf.Max(timing.g + timing.r, 0f); timing.r = 0f; } // Can't start before 0:  keep the end
            texels[rowStart + BattleEffectDataLayout.timing] = timing;

            if (mirrorX || mirrorY)
            {
                Color position = texels[rowStart + BattleEffectDataLayout.position];
                if (mirrorX) { position.r = 1f - position.r; position.b = 1f - position.b; }
                if (mirrorY) { position.g = 1f - position.g; position.a = 1f - position.a; }
                texels[rowStart + BattleEffectDataLayout.position] = position;

                // An anchored lattice's centre is an effect-space point too
                Color tilingSpacing = texels[rowStart + BattleEffectDataLayout.tilingSpacing];
                if (texels[rowStart + BattleEffectDataLayout.tiling].a > 0.5f)
                {
                    if (mirrorX) { tilingSpacing.b = 1f - tilingSpacing.b; }
                    if (mirrorY) { tilingSpacing.a = 1f - tilingSpacing.a; }
                    texels[rowStart + BattleEffectDataLayout.tilingSpacing] = tilingSpacing;
                }

                // Mirroring = an item rotation + a flip of the item's local y (negative scale) ~ local space (shape rotation, clip) is unchanged
                Color rotation = texels[rowStart + BattleEffectDataLayout.rotation];
                rotation.r = MirrorAngle(rotation.r);
                rotation.g = MirrorAngle(rotation.g);
                texels[rowStart + BattleEffectDataLayout.rotation] = rotation;
                if (mirrorX != mirrorY)
                {
                    Color scale = texels[rowStart + BattleEffectDataLayout.scale];
                    scale.g = -scale.g;
                    scale.a = -scale.a;
                    texels[rowStart + BattleEffectDataLayout.scale] = scale;
                }

                // Beat offsets are in effect space (mirrored w/ positions)
                MirrorBeatOffsets(texels, rowStart + BattleEffectDataLayout.beatOffsets01);
                MirrorBeatOffsets(texels, rowStart + BattleEffectDataLayout.beatOffsets23);

                // Stripes are in effect space:  their bands mirror w/ the effect (the across vector's x / y flips)
                Color stripes = texels[rowStart + BattleEffectDataLayout.stripes];
                stripes.g = mirrorX && mirrorY ? stripes.g + Mathf.PI : mirrorX ? -stripes.g : Mathf.PI - stripes.g;
                texels[rowStart + BattleEffectDataLayout.stripes] = stripes;
            }

            if (scaleRatio != Vector2.one || offsetEU != Vector2.zero) { Place(texels, rowStart); }
            if (tiling != null && tiling.IsTiled()) { tiling.WriteData(texels, rowStart, 1f); }

            if (colourSwaps == null || colourSwaps.Count == 0) { return; }
            SwapColour(texels, rowStart + BattleEffectDataLayout.colour);
            SwapColour(texels, rowStart + BattleEffectDataLayout.outlineColour);
            SwapColour(texels, rowStart + BattleEffectDataLayout.altColourB);
            SwapColour(texels, rowStart + BattleEffectDataLayout.altColourC);
            SwapColour(texels, rowStart + BattleEffectDataLayout.altColourD);
            SwapColour(texels, rowStart + BattleEffectDataLayout.edgeColour);
        }

        private void Place(Color[] texels, int rowStart)
        {
            // Scale about the effect's centre, then offset:  p' = 0.5 + (p - 0.5) x ratio + offset
            Color position = texels[rowStart + BattleEffectDataLayout.position];
            position.r = 0.5f + (position.r - 0.5f) * scaleRatio.x + offsetEU.x;
            position.g = 0.5f + (position.g - 0.5f) * scaleRatio.y + offsetEU.y;
            position.b = 0.5f + (position.b - 0.5f) * scaleRatio.x + offsetEU.x;
            position.a = 0.5f + (position.a - 0.5f) * scaleRatio.y + offsetEU.y;
            texels[rowStart + BattleEffectDataLayout.position] = position;

            // Item scale is local (rotated):  each local axis takes the length its direction scales to
            Color rotation = texels[rowStart + BattleEffectDataLayout.rotation];
            Color scale = texels[rowStart + BattleEffectDataLayout.scale];
            Vector2 startRatio = GetLocalScaleRatio(rotation.r), endRatio = GetLocalScaleRatio(rotation.g);
            texels[rowStart + BattleEffectDataLayout.scale] = new Color(scale.r * startRatio.x, scale.g * startRatio.y, scale.b * endRatio.x, scale.a * endRatio.y);

            // Effect-space lengths
            ScalePair(texels, rowStart + BattleEffectDataLayout.beatOffsets01);
            Color spacing = texels[rowStart + BattleEffectDataLayout.tilingSpacing];
            bool isAnchored = texels[rowStart + BattleEffectDataLayout.tiling].a > 0.5f;
            texels[rowStart + BattleEffectDataLayout.tilingSpacing] = new Color(spacing.r * scaleRatio.x, spacing.g * scaleRatio.y,
                isAnchored ? 0.5f + (spacing.b - 0.5f) * scaleRatio.x + offsetEU.x : 0f, isAnchored ? 0.5f + (spacing.a - 0.5f) * scaleRatio.y + offsetEU.y : 0f);
            ScalePair(texels, rowStart + BattleEffectDataLayout.beatOffsets23);
            Color jitter = texels[rowStart + BattleEffectDataLayout.jitter];
            jitter.g *= scaleRatio.x;
            jitter.b *= scaleRatio.y;
            texels[rowStart + BattleEffectDataLayout.jitter] = jitter;

            // Outline thickness:  distances scale by the smaller axis (as in the composite)
            float thicknessRatio = Mathf.Min(scaleRatio.x, scaleRatio.y);
            Color style = texels[rowStart + BattleEffectDataLayout.style];
            style.b *= thicknessRatio;
            style.a *= thicknessRatio;
            texels[rowStart + BattleEffectDataLayout.style] = style;

            // Stripes (bands across uv - 0.5):  the across vector maps through the inverse scale ~ density, angle + phase keep the bands on the items
            Color stripes = texels[rowStart + BattleEffectDataLayout.stripes];
            if (stripes.r <= 0f) { return; }
            var across = new Vector2(-Mathf.Sin(stripes.g) / scaleRatio.x, Mathf.Cos(stripes.g) / scaleRatio.y);
            float acrossLength = across.magnitude;
            across /= acrossLength;
            stripes.r *= acrossLength;
            stripes.g = Mathf.Atan2(-across.x, across.y);
            stripes.a += stripes.r * Vector2.Dot(offsetEU, across);
            texels[rowStart + BattleEffectDataLayout.stripes] = stripes;
        }

        private Vector2 GetLocalScaleRatio(float itemRotation)
        {
            var axisX = new Vector2(Mathf.Cos(itemRotation), Mathf.Sin(itemRotation));
            var axisY = new Vector2(-axisX.y, axisX.x);
            return new Vector2(Vector2.Scale(axisX, scaleRatio).magnitude, Vector2.Scale(axisY, scaleRatio).magnitude);
        }

        private void ScalePair(Color[] texels, int index)
        {
            Color pair = texels[index];
            texels[index] = new Color(pair.r * scaleRatio.x, pair.g * scaleRatio.y, pair.b * scaleRatio.x, pair.a * scaleRatio.y);
        }

        private void MirrorBeatOffsets(Color[] texels, int index)
        {
            Color offsets = texels[index];
            if (mirrorX) { offsets.r = -offsets.r; offsets.b = -offsets.b; }
            if (mirrorY) { offsets.g = -offsets.g; offsets.a = -offsets.a; }
            texels[index] = offsets;
        }

        private float MirrorAngle(float radians)
        {
            if (mirrorX && mirrorY) { return radians + Mathf.PI; }
            return mirrorX ? Mathf.PI - radians : -radians;
        }

        private void SwapColour(Color[] texels, int index)
        {
            Color current = texels[index];
            foreach (BattleEffectColourSwap swap in colourSwaps)
            {
                Color from = swap.GetFrom().linear;
                if (Mathf.Abs(current.r - from.r) + Mathf.Abs(current.g - from.g) + Mathf.Abs(current.b - from.b) + Mathf.Abs(current.a - from.a) > 1e-3f) { continue; }
                texels[index] = swap.GetTo().linear;
                return;
            }
        }
        #endregion
    }
}
