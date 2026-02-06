using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Battle AI", menuName = "BattleAI/BattleAIPriority")]
    public class BattleAIPriority : ScriptableObject
    {
        // Tunables
        [SerializeField] private Skill[] skills; 
        [SerializeField] private BattleAICondition skillCondition;
        [SerializeField] private TargetPriority[] targetPriorities;
        [SerializeField] private bool defaultToRandomTarget = false;

        #region StaticMethods
        public static Skill GetRandomSkill(SkillHandler skillHandler, List<Skill> skillsToExclude, float probabilityToTraverseSkillTree)
        {
            if (skillHandler == null || !skillHandler.HasSkillTree()) { return null; }

            // Simple implementation -- choose at random
            List<SkillBranchMapping> availableBranches = skillHandler.GetAvailableBranchMappings();
            int branchCount = availableBranches.Count;

            if (branchCount > 0)
            {
                int branchIndex = Random.Range(0, branchCount);
                float traverseChance = Random.Range(0f, 1f);
                if (probabilityToTraverseSkillTree >= traverseChance)
                {
                    SkillBranchMapping skillBranchMapping = availableBranches[branchIndex];

                    // Check if skills will exist on traversing
                    var pathSkills = new List<Skill>();
                    skillHandler.GetPathSkills(skillBranchMapping, ref pathSkills);
                    List<Skill> filteredPathSkills = pathSkills.Except(skillsToExclude).ToList();

                    if (filteredPathSkills.Count > 0)
                    {
                        // Walk to next branch, recurse through tree
                        skillHandler.SetBranch(skillBranchMapping, SkillFilterType.None);
                        return GetRandomSkill(skillHandler, skillsToExclude, probabilityToTraverseSkillTree);
                    }
                }
            }

            // Otherwise just select skill from existing options
            List<Skill> skillOptions = skillHandler.GetUnfilteredSkills().Except(skillsToExclude).ToList();
            int skillCount = skillOptions.Count;
            if (skillCount == 0) { return null; }

            int skillIndex = Random.Range(0, skillCount);
            return skillOptions[skillIndex];
        }

        public static void SetRandomTarget(BattleAI battleAI, BattleActionData battleActionData, Skill skill)
        {
            // Randomize input combat participants selections
            battleAI.GetLocalAllies().Shuffle();
            battleAI.GetLocalFoes().Shuffle();
            skill.SetTargets(TargetingNavigationType.Hold, battleActionData, battleAI.GetLocalAllies(), battleAI.GetLocalFoes());
        }
        #endregion

        #region PublicMethods
        public Skill GetSkill(BattleAI battleAI, SkillHandler skillHandler, List<Skill> skillsToExclude)
        {
            if (skills == null || skills.Length == 0) { return null; }

            // Get skills on the combat participant, intersect to skills under consideration
            skillHandler.GetAvailableBranchMappings();
            List<Skill> skillOptions = skillHandler.GetUnfilteredSkills().Except(skillsToExclude).Intersect(skills).ToList();
            int skillCount = skillOptions.Count;
            if (skillCount == 0) { return null; }

            // Check if condition defined viable to pull from subset skill
            bool? skillConditionMet = skillCondition.Check(new[] { battleAI });

            if (skillConditionMet == true)
            {
                int randomSelector = Random.Range(0, skillOptions.Count);
                return skillOptions[randomSelector];
            }
            return null;
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
