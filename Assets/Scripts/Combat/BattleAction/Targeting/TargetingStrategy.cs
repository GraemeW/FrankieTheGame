using System.Collections.Generic;
using System.Linq;
using Frankie.Control;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class TargetingStrategy : ScriptableObject
    {
        [SerializeField] protected CombatParticipantType combatParticipantType;
        [SerializeField] protected FilterStrategy[] filterStrategies;
        
        #region AbstractMethods
        public abstract void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies);
        protected abstract List<BattleEntity> GetBattleEntitiesByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies);
        #endregion
        
        #region ProtectedMethods
        protected static void FilterTargets(BattleActionData battleActionData, FilterStrategy[] tryFilterStrategies)
        {
            if (tryFilterStrategies == null) { return; }
            foreach (FilterStrategy filterStrategy in tryFilterStrategies)
            {
                battleActionData.SetTargets(filterStrategy.Filter(battleActionData.GetTargetsCopy()));
            }
        }
        
        protected static IEnumerable<BattleEntity> GetCombatParticipantsByType(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            if (combatParticipantType is CombatParticipantType.Either or CombatParticipantType.Foe)
            {
                if (activeEnemies != null)
                {
                    foreach (BattleEntity enemy in activeEnemies)
                    {
                        yield return enemy;
                    }
                }
            }
            if (combatParticipantType is CombatParticipantType.Either or CombatParticipantType.Friendly)
            {
                if (activeCharacters != null)
                {
                    foreach (BattleEntity character in activeCharacters)
                    {
                        yield return character;
                    }
                }
            }
        }
        #endregion
        
                #region StaticMethods
        public static TargetingNavigationType ConvertPlayerInputToTargeting(PlayerInputType playerInputType)
        {
            return playerInputType switch
            {
                PlayerInputType.DefaultNone => TargetingNavigationType.Hold,
                PlayerInputType.NavigateUp => TargetingNavigationType.Up,
                PlayerInputType.NavigateLeft => TargetingNavigationType.Left,
                PlayerInputType.NavigateRight => TargetingNavigationType.Right,
                PlayerInputType.NavigateDown => TargetingNavigationType.Down,
                _ => TargetingNavigationType.Hold
            };
        }
        
        public static BattleEntity GetRowShiftedTarget(BattleEntity focalTarget, List<BattleEntity> targets, TargetingNavigationType targetingNavigationType)
        {
            if (targets == null || targets.Count == 0) { return null; }
            if (focalTarget == null) { return targets.FirstOrDefault(); }
            
            BattleRow currentBattleRow = focalTarget.row;
            int currentBattleColumn = focalTarget.column;
            
            BattleEntity tryFocalTarget = focalTarget;
            BattleRow nextBattleRow = currentBattleRow;
            List<BattleEntity> tryBattleEntities = new();
            for (int i = 0; i < BattleMat.GetBattleRowCount(); i++)
            {
                nextBattleRow =  BattleMat.GetNextBattleRow(nextBattleRow, targetingNavigationType);
                tryBattleEntities = targets.Where(target => target.row == nextBattleRow).ToList();
                if (tryBattleEntities.Count > 0) { break; }
            }

            if (currentBattleRow != nextBattleRow)
            {
                tryFocalTarget = tryBattleEntities.OrderBy(target => Mathf.Abs(target.column - currentBattleColumn)).First();
            }
            return tryFocalTarget;
        }

        public static BattleEntity GetColumnShiftedTarget(BattleEntity focalTarget, List<BattleEntity> targets, TargetingNavigationType targetingNavigationType)
        {
            if (targets == null || targets.Count == 0) { return null; }
            if (focalTarget == null) { return targets.FirstOrDefault(); }
            
            List<BattleEntity> tryBattleEntities = targets.Where(target => target.row == focalTarget.row).OrderBy(target => target.column).ToList();
            if (!tryBattleEntities.Contains(focalTarget)) { return null; } // Edge case, not expected to occur
            int currentBattleEntityIndex = tryBattleEntities.IndexOf(focalTarget);
            
            int nextBattleEntityIndex = targetingNavigationType switch
            {
                TargetingNavigationType.Right => (currentBattleEntityIndex == tryBattleEntities.Count - 1) ? 0 : currentBattleEntityIndex + 1,
                TargetingNavigationType.Left => (currentBattleEntityIndex == 0) ? tryBattleEntities.Count - 1 : currentBattleEntityIndex - 1,
                _ => currentBattleEntityIndex
            };
            
            UnityEngine.Debug.Log($"Current battle entity index: {currentBattleEntityIndex} @ column {focalTarget.column} updated to {nextBattleEntityIndex} @ column {tryBattleEntities[nextBattleEntityIndex].column}");
            return tryBattleEntities[nextBattleEntityIndex];
        }
        #endregion
    }
}
