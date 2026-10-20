using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(1)]
    public sealed class BattleEffectSquiggle : BattleEffectShape
    {
        // Note:
        //  - A sine wave segment along the item's local x-axis (rotate the item 90 degrees for vertical)
        //  - Wave runs from its base towards its peak, with the peak's swing pulsing over time (offsets are across the axis)
        //  - Phase advances by phase speed per second

        [SerializeField] private BattleEffectFloatRange frequency = new(30f);
        [SerializeField] private BattleEffectFloatRange waveBase = new(-0.25f);
        [SerializeField] private BattleEffectFloatRange wavePeak = new(0.25f);
        [SerializeField] private BattleEffectFloatRange phaseSpeed = new(1f);
        [SerializeField][Tooltip("How far the segment extends along its axis")] private BattleEffectFloatRange width = new(0.9f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = frequency;
            parameters[1] = waveBase;
            parameters[2] = wavePeak;
            parameters[3] = phaseSpeed;
            parameters[4] = width;
        }
    }
}
