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
        private readonly List<BattleEntity> targets = new();

        public BattleActionData(CombatParticipant sender)
        {
            this.sender = sender;
        }

        #region Getters
        public CombatParticipant GetSender() => sender;
        public bool HasTarget(CombatParticipant combatParticipant) => targets.Any(target => target.combatParticipant == combatParticipant);
        public IList<BattleEntity> GetTargets() => targets.AsReadOnly();
        public List<BattleEntity> GetTargetsCopy() => new(targets);
        public BattleEntity GetFocalTarget() => focalTarget;
        public bool HasTargets() => targets.Count > 0;
        #endregion

        #region Setters
        public void SetFocalTarget(TargetingNavigationType targetingNavigationType)
        {
            if (targets.Count == 0) { return; }
            if (focalTarget == null || !targets.Contains(focalTarget)) { SetDefaultFocalTarget(); return; }
            
            switch (targetingNavigationType)
            {
                case TargetingNavigationType.Right:
                case TargetingNavigationType.Left:
                    focalTarget = TargetingStrategy.GetColumnShiftedTarget(focalTarget, targets, targetingNavigationType);
                    return;
                case TargetingNavigationType.Up:
                case TargetingNavigationType.Down:
                    focalTarget = TargetingStrategy.GetRowShiftedTarget(focalTarget, targets, targetingNavigationType);
                    return;
                case TargetingNavigationType.Hold:
                default:
                    return;
            }
        }
        
        public void SetTargets(BattleEntity target)
        {
            if (target == null) { ClearTargets(); return; }
            
            ClearTargets();
            focalTarget = target;
            targets.Add(target);
        }
        
        public void SetTargets(IEnumerable<BattleEntity> setTargets)
        {
            if (setTargets == null) { ClearTargets(); return; }

            targets.Clear();
            foreach (BattleEntity target in setTargets) { targets.Add(target); }
            if (focalTarget == null) { SetDefaultFocalTarget(); }
        }
        
        public void SetTargetFromFocalTarget()
        {
            if (focalTarget == null) { SetDefaultFocalTarget(); }
            if (focalTarget == null) { ClearTargets(); return; }
            
            targets.Clear();
            targets.Add(focalTarget);
        }

        private void ClearTargets()
        {
            focalTarget = null;
            targets.Clear();
        }
        #endregion

        #region Utility
        public void StartCoroutine(IEnumerator coroutine)
        {
            sender.GetComponent<MonoBehaviour>().StartCoroutine(coroutine);
        }
        
        private void SetDefaultFocalTarget()
        {
            List<BattleEntity> tryBattleEntities = targets.Where(target => target.row == BattleMat.GetDefaultBattleRow()).ToList();
            switch (tryBattleEntities.Count)
            {
                case 1:
                    focalTarget = tryBattleEntities[0]; 
                    return;
                case > 1:
                    focalTarget = tryBattleEntities.OrderBy(target => Mathf.Abs(target.column - BattleMat.GetDefaultBattleColumn())).First();
                    return;
                default:
                    focalTarget = targets.OrderBy(target => Mathf.Abs(target.column - BattleMat.GetDefaultBattleColumn())).First();
                    return;
            }
        }
        #endregion
    }
}
