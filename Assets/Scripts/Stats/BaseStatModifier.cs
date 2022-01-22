using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [System.Serializable]
    public struct BaseStatModifier
    {
        public Stat stat;
        public float minValue;
        public float maxValue;
    }
}