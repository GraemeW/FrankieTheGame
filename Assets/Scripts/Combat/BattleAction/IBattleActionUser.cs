using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public interface IBattleActionUser
    {
        public void Use(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action finished);
        public IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies);
        public bool IsItem();
        public string GetName();
    }
}