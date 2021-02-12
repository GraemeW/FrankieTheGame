using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "Skills/New Status Effect")]
    public class StatusEffect : ScriptableObject
    {
        public StatusEffectType statusEffectType;
        public float value;
        public float timer;
        public int numberOfTicks;
    }
}
