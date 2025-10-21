using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Frankie.Combat
{
    public class BattleActionData
    {
        private readonly CombatParticipant sender;
        private BattleEntity focalTarget;
        private List<BattleEntity> targets = new();
        public int targetCount;

        public BattleActionData(CombatParticipant sender)
        {
            this.sender = sender;
        }

        #region Getters
        public CombatParticipant GetSender() => sender;
        public bool HasTarget(CombatParticipant combatParticipant) => targets.Any(target => target.combatParticipant == combatParticipant);
        public List<BattleEntity> GetTargets() => targets;
        public BattleEntity GetFirst() => targets.FirstOrDefault();
        public BattleEntity GetLast() => targets.LastOrDefault();
        #endregion

        #region Setters
        public void SetTargets(BattleEntity target)
        {
            ClearTargets();
            focalTarget = target;
            targets.Add(target);
            targetCount = 1;
        }

        public void SetFocalTarget(BattleEntity setFocalTarget)
        {
            focalTarget = setFocalTarget;
        }
        
        public void SetTargets(IEnumerable<BattleEntity> setTargets)
        {
            if (setTargets == null) { ClearTargets(); return; }
            targets = setTargets.ToList();;
            targetCount = targets.Count;
        }
        
        public void ClearTargets()
        {
            focalTarget = null;
            targets.Clear();
            targetCount = 0;
        }
        #endregion

        #region Utility
        public void ReverseTargets()
        {
            targets.Reverse();
        }
        
        public void StartCoroutine(IEnumerator coroutine)
        {
            sender.GetComponent<MonoBehaviour>().StartCoroutine(coroutine);
        }
        #endregion
    }
}
