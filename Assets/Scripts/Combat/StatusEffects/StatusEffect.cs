using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "Skills/New Status Effect")]
    public class StatusEffect : ScriptableObject
    {
        public StatusEffectType statusEffectType;
        public float primaryValue;
        public float secondaryValue;
        public float duration;
        public int numberOfTicks;
    }
}
