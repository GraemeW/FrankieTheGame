using System;
using UnityEngine;

namespace Frankie.Combat
{
    [Serializable]
    public struct BattleEffectFloatRange
    {
        // Note:
        //  - Runs from start to end over a battle effect item's (eased) duration
        //  - Equal values = constant

        // Tunables
        [SerializeField] private float start;
        [SerializeField] private float end;

        // Editor Field References
        public const string startField = nameof(start);
        public const string endField = nameof(end);

        // Constructors
        public BattleEffectFloatRange(float value) : this(value, value) { }
        public BattleEffectFloatRange(float start, float end)
        {
            this.start = start;
            this.end = end;
        }

        // Public Methods
        public float GetStart() => start;
        public float GetEnd() => end;
    }
}
