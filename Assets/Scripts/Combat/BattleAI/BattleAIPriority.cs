using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Battle AI", menuName = "Combat/BattleAI/BattleAIPriority", order = 5)]
    public class BattleAIPriority : ScriptableObject
    {
        // Tunables
        [SerializeField] private Skill[] skills; 
        [SerializeField] private BattleAICondition skillCondition;
        [SerializeField] private TargetPriority[] targetPriorities;
        [SerializeField] private bool defaultToRandomTarget = false;

        #region StaticMethods
        public static void SetRandomTarget(BattleAI battleAI, BattleActionData battleActionData, Skill skill)
        {
            // Randomize input combat participants selections
            battleAI.GetLocalAllies().Shuffle();
            battleAI.GetLocalFoes().Shuffle();
            skill.SetTargets(TargetingNavigationType.Hold, battleActionData, battleAI.GetLocalAllies(), battleAI.GetLocalFoes());
        }
        #endregion

        #region PublicMethods
        public bool TryGetSkill(BattleAI battleAI, SkillHandler skillHandler, List<Skill> skillsToExclude, bool filterByAP, out Skill skill)
        {
            skill = null;
            if (skills == null || skills.Length == 0) { return false; }
            if (!skillCondition.Check(new[] { battleAI })) { return false; }

            return skillHandler.TryGetRandomSkill(skillsToExclude, filterByAP, skills, out skill);
        }

        public void SetTarget(BattleAI battleAI, BattleActionData battleActionData, Skill skill)
        {
            // Early exit (i.e. set with skill condition, but no specific target)
            if (targetPriorities == null || targetPriorities.Length == 0)
            {
                if (defaultToRandomTarget) { SetRandomTarget(battleAI, battleActionData, skill); } 
                return;
            }

            // Specific target set (standard behaviour)
            if (targetPriorities.Any(targetPriority => targetPriority.SetTarget(battleAI, battleActionData, skill)))
            {
                return;
            }

            // Default fallback state
            if (defaultToRandomTarget)
            {
                SetRandomTarget(battleAI, battleActionData, skill);
            } 
        }
        #endregion
    }
}
