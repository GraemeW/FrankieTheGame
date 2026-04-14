using System;
using System.Collections.Generic;
using System.Linq;
using Frankie.Utils;

namespace Frankie.Stats
{
    public class SkillStatAttribute : RestrictedEnumAttribute
    {
        // Tunables + Cache
        private static readonly Stat[] _nonSkillStats =
        { 
            Stat.HP, 
            Stat.AP, 
            Stat.InitialLevel, 
            Stat.ExperienceReward, 
            Stat.ExperienceToLevelUp
        };
        
        // Private Cache
        private static readonly Stat[] _allStats = Enum.GetValues(typeof(Stat)).Cast<Stat>().ToArray();
        private static readonly HashSet<Stat> _nonSkillStatsHash = _nonSkillStats.ToHashSet();
        private static readonly Stat[] _skillStats = _allStats.Where(stat => !_nonSkillStatsHash.Contains(stat)).ToArray();

        // Constructor
        public SkillStatAttribute() : base(Array.ConvertAll(_nonSkillStats, v => (int)v)) { }
        
        #region PublicMethods
        public IList<Stat> GetSkillStats() => _skillStats.ToList();
        #endregion
    }
}
