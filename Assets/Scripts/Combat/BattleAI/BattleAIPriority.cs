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
        [SerializeField][SkillReference] private List<string> skillReferences = new(); // Skill names ~ resolved through the Skill addressables cache (same as SkillBranch)
        [SerializeField] private BattleAICondition skillCondition;
        [SerializeField] private List<TargetPriority> targetPriorities = new();
        [SerializeField] private bool defaultToRandomTarget = false;

        // Editor Field References
        internal const string skillReferencesField = nameof(skillReferences);

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
            if (skillReferences == null || skillReferences.Count == 0) { return false; }
            if (!skillCondition.Check(new[] { battleAI })) { return false; }

            Skill[] skills = skillReferences.Select(Skill.GetSkillFromName).Where(resolvedSkill => resolvedSkill != null).ToArray();
            return skills.Length > 0 && skillHandler.TryGetRandomSkill(skillsToExclude, filterByAP, skills, out skill);
        }

        public void SetTarget(BattleAI battleAI, BattleActionData battleActionData, Skill skill)
        {
            // Early exit (i.e. set with skill condition, but no specific target)
            if (targetPriorities == null || targetPriorities.Count == 0)
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
