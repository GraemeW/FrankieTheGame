using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public interface IBattleActionUser
    {
        public bool Use(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action finished);
        public List<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies);
        public bool IsItem();
        public string GetName();
    }
}
