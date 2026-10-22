using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectBeats
    {
        // Notes:
        //  - Up to 4 held steps within one item, each moving it (position offset), stretching it (elongation) + recolouring it (palette index)
        //  - Each beat holds from its start until the next one starts
        //  - Applied on top of the item's ranges (which still run over its duration)

        [SerializeField][Tooltip("Beats in use (0 = off)")][Range(0, BattleEffectDataLayout.maxBeats)] private int count = 0;
        [SerializeField] private BattleEffectBeat beatA;
        [SerializeField] private BattleEffectBeat beatB;
        [SerializeField] private BattleEffectBeat beatC;
        [SerializeField] private BattleEffectBeat beatD;

        // Editor Field References
        public const string countField = nameof(count);
        public const string beatAField = nameof(beatA);
        public const string beatBField = nameof(beatB);
        public const string beatCField = nameof(beatC);
        public const string beatDField = nameof(beatD);

        // Constants
        private const float _neverSeconds = 1e9f;

        #region InternalMethods
        internal bool IsPaletteUsed()
        {
            for (int i = 0; i < GetCount(); i++) { if (GetBeat(i).GetColourIndex() > 0) { return true; } }
            return false;
        }

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            var starts = new Color(_neverSeconds, _neverSeconds, _neverSeconds, _neverSeconds);
            var colours = new Color(0f, 0f, 0f, 0f);
            var offsets = new Vector2[BattleEffectDataLayout.maxBeats];
            var elongations = new Vector2[BattleEffectDataLayout.maxBeats];
            for (int i = 0; i < GetCount(); i++)
            {
                BattleEffectBeat beat = GetBeat(i);
                starts[i] = beat.GetStartSeconds();
                colours[i] = beat.GetColourIndex();
                offsets[i] = beat.GetOffset() * effectScale;
                elongations[i] = beat.GetElongation();
            }
            texels[rowStart + BattleEffectDataLayout.beatStarts] = starts;
            texels[rowStart + BattleEffectDataLayout.beatColours] = colours;
            texels[rowStart + BattleEffectDataLayout.beatOffsets01] = new Color(offsets[0].x, offsets[0].y, offsets[1].x, offsets[1].y);
            texels[rowStart + BattleEffectDataLayout.beatOffsets23] = new Color(offsets[2].x, offsets[2].y, offsets[3].x, offsets[3].y);
            texels[rowStart + BattleEffectDataLayout.beatElongations01] = new Color(elongations[0].x, elongations[0].y, elongations[1].x, elongations[1].y);
            texels[rowStart + BattleEffectDataLayout.beatElongations23] = new Color(elongations[2].x, elongations[2].y, elongations[3].x, elongations[3].y);
        }

        internal static void WriteEmpty(Color[] texels, int rowStart)
        {
            texels[rowStart + BattleEffectDataLayout.beatStarts] = new Color(_neverSeconds, _neverSeconds, _neverSeconds, _neverSeconds);
        }
        #endregion

        #region PrivateMethods
        private int GetCount() => Mathf.Clamp(count, 0, BattleEffectDataLayout.maxBeats);
        private BattleEffectBeat GetBeat(int index) => index switch { 0 => beatA, 1 => beatB, 2 => beatC, _ => beatD };
        #endregion
    }
}
