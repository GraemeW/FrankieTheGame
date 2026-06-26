using System;
using System.Collections.Generic;

namespace Frankie.Stats
{
    [Serializable]
    public class BaseStatsSaveData
    {
        public int level;
#pragma warning disable UAC1009
        // Unity serialization error, but serialization is OK by Newtonsoft
        public Dictionary<Stat, float> statSheet;
#pragma warning restore UAC1009

        public BaseStatsSaveData(int level, Dictionary<Stat, float> statSheet)
        {
            this.level = level;
            this.statSheet = statSheet;
        }
    }
}
