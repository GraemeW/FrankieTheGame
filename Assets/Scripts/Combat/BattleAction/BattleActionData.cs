using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Frankie.Combat
{
    public class BattleActionData
    {
        CombatParticipant sender;
        List<BattleEntity> targets = new List<BattleEntity>();
        public int targetCount;

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

        public void SetTargets(IEnumerable<BattleEntity> targets)
        {
            if (targets == null) { this.targets.Clear(); targetCount = 0; return; }

            List<BattleEntity> targetList = targets.ToList();
            SetTargets(targetList);
        }

        public void SetTargets(List<BattleEntity> targets)
        {
            if (targets == null) { this.targets.Clear(); targetCount = 0; return; }

            this.targets = targets;
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

        public IEnumerable<BattleEntity> GetTargets()
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

