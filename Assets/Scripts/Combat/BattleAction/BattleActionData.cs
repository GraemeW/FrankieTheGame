using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Frankie.Combat
{
    public class BattleActionData
    {
        private readonly CombatParticipant sender;
        private List<BattleEntity> targets = new();
        public int targetCount;
        public int enemiesAddedDuringCombat;

        public BattleActionData(CombatParticipant sender)
        {
            this.sender = sender;
        }

        public CombatParticipant GetSender()
        {
            return sender;
        }

        public void SetTargets(BattleEntity target)
        {
            targets.Clear();
            targets.Add(target);
            targetCount = 1;
        }

        public void SetTargets(IEnumerable<BattleEntity> setTarget)
        {
            if (setTarget == null) { targets.Clear(); targetCount = 0; return; }

            var targetList = setTarget.ToList();
            targets = targetList;
            targetCount = targets.Count;
        }

        public void ClearTargets()
        {
            targetCount = 0;
            targets.Clear();
        }

        public void ReverseTargets()
        {
            targets.Reverse();
        }

        public bool HasTarget(CombatParticipant combatParticipant)
        {
            foreach (BattleEntity target in targets)
            {
                if (target.combatParticipant == combatParticipant) { return true; }
            }
            return false;
        }

        public List<BattleEntity> GetTargets()
        {
            return targets;
        }

        public BattleEntity GetFirst()
        {
            return targets.FirstOrDefault();
        }

        public BattleEntity GetLast()
        {
            return targets.LastOrDefault();
        }

        public void StartCoroutine(IEnumerator coroutine)
        {
            sender.GetComponent<MonoBehaviour>().StartCoroutine(coroutine);
        }
    }
}
