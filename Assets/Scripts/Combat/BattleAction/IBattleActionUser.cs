using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public interface IBattleActionSuper
    {
        public bool Use(BattleActionData battleActionData, Action finished);
        public void GetTargets(bool? traverseForward, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies);
        public bool IsItem();
        public string GetName();
    }
}
