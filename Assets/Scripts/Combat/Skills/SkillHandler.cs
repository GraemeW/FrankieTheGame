using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    [RequireComponent(typeof(BaseStats))]
    public class SkillHandler : MonoBehaviour
    {
        // Tunables
        [SerializeField] SkillTree skillTree = null;
        [SerializeField] float skillTreeLevelMultiplierForStatUnlock = 10f;

        // State
        SkillBranch currentBranch = null;
        Skill activeSkill = null;
        int skillTreeLevel = 0;

        // Cached References
        BaseStats baseStats = null;

        #region UnityMethods
        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
        }
        #endregion

        #region PlayerBehaviour
        public Skill GetActiveSkill()
        {
            return activeSkill;
        }

        public void GetSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down)
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            up = FilterSkillByTrait(currentBranch.GetSkill(SkillBranchMapping.up));
            left = FilterSkillByTrait(currentBranch.GetSkill(SkillBranchMapping.left));
            right = FilterSkillByTrait(currentBranch.GetSkill(SkillBranchMapping.right));
            down = FilterSkillByTrait(currentBranch.GetSkill(SkillBranchMapping.down));
        }

        public void SetBranchOrSkill(SkillBranchMapping skillBranchMapping)
        {
            // Attempts to set branch first;  otherwise sets skill
            if (SetBranch(skillBranchMapping)) { return; }
            if (SetSkill(skillBranchMapping)) { return; }
        }

        public bool SetBranch(SkillBranchMapping skillBranchMapping)
        {
            if (currentBranch == null) { ResetCurrentBranch(); return true; }

            if (currentBranch.HasBranch(skillBranchMapping)) 
            {
                // Check if available skills exist after filtering
                Skill tryActiveSkill = FilterSkillByTrait(currentBranch.GetSkill(skillBranchMapping));
                if (tryActiveSkill == null) { return false; }
                else { activeSkill = tryActiveSkill; }

                // Check if meaningful to traverse branch after filtering
                SkillBranch tryCurrentBranch = skillTree.GetSkillBranchFromID(currentBranch.GetBranch(skillBranchMapping));
                int availableSkillCount = GetAvailableSkills(tryCurrentBranch, skillTreeLevel + 1).Count;

                if (availableSkillCount == 0) { return false; }

                currentBranch = tryCurrentBranch;
                skillTreeLevel++;
                return true;
            }
            return false;
        }

        private bool SetSkill(SkillBranchMapping skillBranchMapping)
        {
            if (currentBranch.HasSkill(skillBranchMapping))
            {
                Skill tryActiveSkill = FilterSkillByTrait(currentBranch.GetSkill(skillBranchMapping));
                if (tryActiveSkill == null){ return false; }
                else 
                { 
                    activeSkill = tryActiveSkill; 
                    return true; 
                }
            }
            return false;
        }
        #endregion

        #region Utility
        public List<Skill> GetUnfilteredSkills(SkillBranch skillBranch)
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            List<Skill> availableSkills = new List<Skill>();
            foreach (Skill skill in skillBranch.GetAllSkills())
            {
                if (skill != null)
                {
                    availableSkills.Add(skill);
                }
            }

            return availableSkills;
        }

        public List<Skill> GetUnfilteredSkills()
        {
            return GetUnfilteredSkills(currentBranch);
        }

        public List<Skill> GetAvailableSkills(SkillBranch skillBranch, int filterLevel = -1)
        {
            List<Skill> availableSkills = new List<Skill>();
            foreach (Skill skill in skillBranch.GetAllSkills())
            {
                Skill filteredSkill = FilterSkillByTrait(skill, filterLevel);
                if (filteredSkill != null)
                {
                    availableSkills.Add(filteredSkill);
                }
            }

            return availableSkills;
        }

        public List<Skill> GetAvailableSkills()
        {
            return GetAvailableSkills(currentBranch);
        }

        public List<SkillBranchMapping> GetAvailableBranchMappings()
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            List<SkillBranchMapping> availableBranches = new List<SkillBranchMapping>();
            if (currentBranch.HasBranch(SkillBranchMapping.up)) { availableBranches.Add(SkillBranchMapping.up); }
            if (currentBranch.HasBranch(SkillBranchMapping.left)) { availableBranches.Add(SkillBranchMapping.left); }
            if (currentBranch.HasBranch(SkillBranchMapping.right)) { availableBranches.Add(SkillBranchMapping.right); }
            if (currentBranch.HasBranch(SkillBranchMapping.down)) { availableBranches.Add(SkillBranchMapping.down); }

            return availableBranches;
        }

        private Skill FilterSkillByTrait(Skill skill, int levelForEvaluation = -1)
        {
            if (skill == null) { return null; }
            if (levelForEvaluation == -1) { levelForEvaluation = skillTreeLevel; }

            SkillStat skillStat = skill.GetStat();
            if (skillStat == SkillStat.None) { return null; }

            if (Enum.TryParse(skillStat.ToString(), out Stat stat))
            {
                float value = baseStats.GetStat(stat);
                return (value >= levelForEvaluation * skillTreeLevelMultiplierForStatUnlock) ? skill : null;
            }
            return null;
        }

        public void ResetCurrentBranch()
        {
            currentBranch = skillTree.GetRootSkillBranch();
            activeSkill = null;
            skillTreeLevel = 0;
        }
        #endregion
    }
}
