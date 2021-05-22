using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [System.Serializable]
    public struct StatusEffectProbabilityPair
    {
        public StatusEffect statusEffect;
        [Range(0, 1)] public float fractionalProbability;
    }
}