using System;
using System.Collections.Generic;

namespace Frankie.Combat
{
    public interface IBattleActionSuper
    {
        public bool Use(BattleActionData battleActionData, Action finished);
        public void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies);
        public bool IsItem();
        public string GetName();
    }
}
