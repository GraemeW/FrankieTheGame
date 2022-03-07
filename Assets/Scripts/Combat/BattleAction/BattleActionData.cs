using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Frankie.Combat
{
    public class BattleActionData
    {
        CombatParticipant sender;
        List<CombatParticipant> targets = new List<CombatParticipant>();
        public int targetCount;

        public BattleActionData(CombatParticipant sender)
        {
            this.sender = sender;
        }

        public CombatParticipant GetSender()
        {
            return sender;
        }

        public void SetTargets(CombatParticipant target)
        {
            targets.Clear();
            targets.Add(target);
            targetCount = 1;
        }

        public void SetTargets(IEnumerable<CombatParticipant> targets)
        {
            if (targets == null) { this.targets.Clear(); targetCount = 0; return; }

            List<CombatParticipant> targetList = targets.ToList();
            SetTargets(targetList);
        }

        public void SetTargets(List<CombatParticipant> targets)
        {
            if (targets == null) { this.targets.Clear(); targetCount = 0; return; }

            this.targets = targets;
            targetCount = targets.Count;
        }

        public void ClearTargets()
        {
            targets.Clear();
        }

        public void ReverseTargets()
        {
            targets.Reverse();
        }

        public IEnumerable<CombatParticipant> GetTargets()
        {
            return targets;
        }

        public CombatParticipant GetFirst()
        {
            return targets.FirstOrDefault();
        }

        public CombatParticipant GetLast()
        {
            return targets.LastOrDefault();
        }

        public void StartCoroutine(IEnumerator coroutine)
        {
            sender.GetComponent<MonoBehaviour>().StartCoroutine(coroutine);
        }
    }
}

