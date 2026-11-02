using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public struct BattleEffectVector2Range
    {
        // Note:
        //  - Runs from start to end over a battle effect item's (eased) duration
        //  - Equal values = constant

        // Tunables
        [SerializeField] private Vector2 start;
        [SerializeField] private Vector2 end;

        // Editor Field References
        public const string startField = nameof(start);
        public const string endField = nameof(end);

        // Constructors
        public BattleEffectVector2Range(Vector2 value) : this(value, value) { }
        public BattleEffectVector2Range(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }

        // Public Methods
        public Vector2 GetStart() => start;
        public Vector2 GetEnd() => end;
    }
}
