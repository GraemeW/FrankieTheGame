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
        //      - the instanced recipe's own instances come along (up to `maxInstanceDepth` levels deep, per BattleEffectRecipe)
        //  - Global recipe parameters (scale ratio, frame rate, edge softness + glow) derive from the original recipe holder

        // Tunables
        [SerializeField][Tooltip("Inspector name only")] private string label = "";
        [SerializeField] private BattleEffectRecipe recipe;
        [SerializeField][Tooltip("Added to every instanced item's start")][Min(0f)] private float startOffsetSeconds = 0f;
        [SerializeField][Tooltip("Left <-> right, about the effect's centre")] private bool mirrorX = false;
        [SerializeField][Tooltip("Top <-> bottom, about the effect's centre")] private bool mirrorY = false;
        [SerializeField][Tooltip("Replaces item colours (colour, outline colour, alt colours + edge gradient colour)")] private List<BattleEffectColourSwap> colourSwaps = new();

        // Constructors
        public BattleEffectInstance() { }
        public BattleEffectInstance(BattleEffectRecipe recipe, float startOffsetSeconds, bool mirrorX, bool mirrorY, IEnumerable<BattleEffectColourSwap> colourSwaps)
        {
            this.recipe = recipe;
            this.startOffsetSeconds = startOffsetSeconds;
            this.mirrorX = mirrorX;
            this.mirrorY = mirrorY;
            this.colourSwaps = new List<BattleEffectColourSwap>(colourSwaps);
        }

        #region Public Methods
        public string GetLabel() => label;
        public BattleEffectRecipe GetRecipe() => recipe;
        public float GetStartOffset() => startOffsetSeconds;
        #endregion
        
        #region PrivateInternalMethods
        internal void ApplyTo(Color[] texels, int rowStart)
        {
            // Re-times, mirrors + recolours one baked item row
            Color timing = texels[rowStart + BattleEffectDataLayout.timing];
            timing.r += startOffsetSeconds;
            texels[rowStart + BattleEffectDataLayout.timing] = timing;

            if (mirrorX || mirrorY)
            {
                Color position = texels[rowStart + BattleEffectDataLayout.position];
                if (mirrorX) { position.r = 1f - position.r; position.b = 1f - position.b; }
                if (mirrorY) { position.g = 1f - position.g; position.a = 1f - position.a; }
                texels[rowStart + BattleEffectDataLayout.position] = position;

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

            if (colourSwaps == null || colourSwaps.Count == 0) { return; }
            SwapColour(texels, rowStart + BattleEffectDataLayout.colour);
            SwapColour(texels, rowStart + BattleEffectDataLayout.outlineColour);
            SwapColour(texels, rowStart + BattleEffectDataLayout.altColourB);
            SwapColour(texels, rowStart + BattleEffectDataLayout.altColourC);
            SwapColour(texels, rowStart + BattleEffectDataLayout.altColourD);
            SwapColour(texels, rowStart + BattleEffectDataLayout.edgeColour);
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
