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
                Skill tryActiveSkill = FilterSkillByTrait(currentBranch.GetSkill(skillBranchMapping));
                if (tryActiveSkill == null) { return false; }
                else { activeSkill = tryActiveSkill; }

                currentBranch = skillTree.GetSkillBranchFromID(currentBranch.GetBranch(skillBranchMapping));
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

        private Skill FilterSkillByTrait(Skill skill)
        {
            SkillStat skillStat = skill.GetStat();
            if (skillStat == SkillStat.None) { return null; }

            if (Enum.TryParse(skillStat.ToString(), out Stat stat))
            {
                float value = baseStats.GetStat(stat);
                return (value >= skillTreeLevel * skillTreeLevelMultiplierForStatUnlock) ? skill : null;
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

        #region AIBehaviour
        public List<Skill> GetAvailableSkills()
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            List<Skill> availableSkills = new List<Skill>();
            if (currentBranch.HasSkill(SkillBranchMapping.up)) { availableSkills.Add(currentBranch.GetSkill(SkillBranchMapping.up)); }
            if (currentBranch.HasSkill(SkillBranchMapping.left)) { availableSkills.Add(currentBranch.GetSkill(SkillBranchMapping.left)); }
            if (currentBranch.HasSkill(SkillBranchMapping.right)) { availableSkills.Add(currentBranch.GetSkill(SkillBranchMapping.right)); }
            if (currentBranch.HasSkill(SkillBranchMapping.down)) { availableSkills.Add(currentBranch.GetSkill(SkillBranchMapping.down)); }

            return availableSkills;
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
        #endregion
    }
}
