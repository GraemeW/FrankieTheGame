using System;
using UnityEngine;

namespace Frankie.Combat
{
    [Serializable]
    public sealed class BattleEffectSquiggle
    {
        // Note:
        //  - Positions / sizes in UV (0-1 over the effect, v upwards)
        //  - Travel runs linearly from start to end over the shape's duration
        //  - Phase advances by phase speed per second && the wave's height pulses between the wave range over time

        [SerializeField] private bool isHorizontal = false;
        [SerializeField] private float startTravel = 0f;
        [SerializeField] private float endTravel = 1.5f;
        [SerializeField][Min(0.0001f)] private float width = 0.9f;
        [SerializeField] private float frequency = 30f;
        [SerializeField] private Vector2 waveRange = new(0.25f, 0.75f);
        [SerializeField] private float phaseSpeed = 1f;
        [SerializeField][Min(0f)] private float thickness = 0.2f;

        public void GetParameters(out bool isVariant, out Vector4 paramsA, out Vector4 paramsB)
        {
            isVariant = isHorizontal;
            paramsA = new Vector4(startTravel, endTravel, width, frequency);
            paramsB = new Vector4(waveRange.x, waveRange.y, phaseSpeed, thickness);
        }
    }
}
