using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public struct BattleEffectColourSwap
    {
        // Used to match + swap ALL of an item's colours (its colour, colour cycle + edge gradient)

        [SerializeField] private Color from;
        [SerializeField] private Color to;

        // Editor Field References
        internal const string fromField = nameof(from);
        internal const string toField = nameof(to);

        public BattleEffectColourSwap(Color from, Color to)
        {
            this.from = from;
            this.to = to;
        }

        public Color GetFrom() => from;
        public Color GetTo() => to;
    }
}
