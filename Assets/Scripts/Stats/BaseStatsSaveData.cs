using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [Serializable]
    public class BaseStatsSaveData
    {
        public int level;
        // ReSharper disable once Unity.RedundantSerializeFieldAttribute - Required Dictionary (even if public) 
        [SerializeField] public Dictionary<Stat, float> statSheet;

        public BaseStatsSaveData(int level, Dictionary<Stat, float> statSheet)
        {
            this.level = level;
            this.statSheet = statSheet;
        }
    }
}
