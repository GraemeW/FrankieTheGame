using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Battle AI", menuName = "BattleAI/BattleAIPriority")]
    public class BattleAIPriority : ScriptableObject
    {
        // Tunables
        [SerializeField] SkillPriority[] skillPriorities = null;
        [SerializeField] TargetPriority[] targetPriorities = null;

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
                    List<Skill> pathSkills = new List<Skill>();
                    skillHandler.GetPathSkills(skillBranchMapping, ref pathSkills);
                    List<Skill> filteredPathSkills = pathSkills.Except(skillsToExclude).ToList();

                    if (filteredPathSkills.Count > 0)
                    {
                        // Walk to next branch, recurse through tree
                        skillHandler.SetBranch(skillBranchMapping, SkillFilterType.None);
                        return BattleAIPriority.GetRandomSkill(skillHandler, skillsToExclude, probabilityToTraverseSkillTree);
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

        public static void SetRandomTarget(BattleActionData battleActionData, Skill skill, bool isFriendly, List<CombatParticipant> characters, List<CombatParticipant> enemies)
        {
            // Randomize input combat participants selections
            characters.Shuffle();
            enemies.Shuffle();

            if (isFriendly)
            {
                skill.GetTargets(true, battleActionData, characters, enemies);
            }
            else
            {
                skill.GetTargets(true, battleActionData, enemies, characters);
            }
        }
        #endregion

        #region PublicMethods
        public Skill GetSkill(SkillHandler skillHandler, List<Skill> skillsToExclude, float probabilityToTraverseSkillTree)
        {
            if (skillPriorities == null || skillPriorities.Length == 0) { return null; }
            
            foreach (SkillPriority skillPriority in skillPriorities)
            {
                Skill skill = skillPriority.GetSkill(skillHandler, skillsToExclude, probabilityToTraverseSkillTree);
                if (skill != null) { return skill; }
            }
            return null;
        }

        public void SetTarget(BattleActionData battleActionData, Skill skill, bool isFriendly, List<CombatParticipant> characters, List<CombatParticipant> enemies)
        {
            if (targetPriorities == null || targetPriorities.Length == 0) { return; }

            foreach (TargetPriority targetPriority in targetPriorities)
            {
                if (targetPriority.SetTarget(battleActionData, skill, isFriendly, characters, enemies)) { return; }
            }
        }
        #endregion
    }
}
