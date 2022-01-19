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
        [SerializeField][Min(0)] float skillTreeLevelMultiplierForStatUnlock = 10f;

        // State
        SkillBranch currentBranch = null;
        Skill activeSkill = null;
        int skillTreeLevel = 0;

        // Cached References
        CombatParticipant combatParticipant = null;
        BaseStats baseStats = null;

        #region UnityMethods
        private void Awake()
        {
            combatParticipant = GetComponent<CombatParticipant>();
            baseStats = GetComponent<BaseStats>();
        }
        #endregion

        #region StandardMethods
        public Skill GetActiveSkill()
        {
            return activeSkill;
        }

        public void GetPlayerSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down)
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            up = FilterSkill(currentBranch.GetSkill(SkillBranchMapping.up), SkillFilterType.All);
            left = FilterSkill(currentBranch.GetSkill(SkillBranchMapping.left), SkillFilterType.All);
            right = FilterSkill(currentBranch.GetSkill(SkillBranchMapping.right), SkillFilterType.All);
            down = FilterSkill(currentBranch.GetSkill(SkillBranchMapping.down), SkillFilterType.All);
        }

        public void SetBranchOrSkill(SkillBranchMapping skillBranchMapping, SkillFilterType skillFilterType)
        {
            // Attempts to set branch first;  otherwise sets skill
            if (SetBranch(skillBranchMapping, skillFilterType)) { return; }
            if (SetSkill(skillBranchMapping, skillFilterType)) { return; }
        }

        public bool SetBranch(SkillBranchMapping skillBranchMapping, SkillFilterType skillFilterType)
        {
            if (currentBranch == null) { ResetCurrentBranch(); return true; }

            if (currentBranch.HasBranch(skillBranchMapping)) 
            {
                // Check if available skills exist after filtering
                Skill tryActiveSkill = FilterSkill(currentBranch.GetSkill(skillBranchMapping), SkillFilterType.All);
                if (tryActiveSkill == null) { return false; }
                else { activeSkill = tryActiveSkill; }

                // Check if meaningful to traverse branch after filtering
                SkillBranch tryCurrentBranch = skillTree.GetSkillBranchFromID(currentBranch.GetBranch(skillBranchMapping));
                int availableSkillCount = GetAvailableSkills(tryCurrentBranch, skillFilterType, skillTreeLevel + 1).Count;

                if (availableSkillCount == 0) { return false; }

                currentBranch = tryCurrentBranch;
                skillTreeLevel++;
                return true;
            }
            return false;
        }

        private bool SetSkill(SkillBranchMapping skillBranchMapping, SkillFilterType skillFilterType)
        {
            if (currentBranch.HasSkill(skillBranchMapping))
            {
                Skill tryActiveSkill = FilterSkill(currentBranch.GetSkill(skillBranchMapping), SkillFilterType.All);
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

        public List<Skill> GetAvailableSkills(SkillBranch skillBranch, SkillFilterType skillFilterType, int filterLevel = -1)
        {
            List<Skill> availableSkills = new List<Skill>();
            foreach (Skill skill in skillBranch.GetAllSkills())
            {
                Skill filteredSkill = FilterSkill(skill, skillFilterType, filterLevel);
                if (filteredSkill != null)
                {
                    availableSkills.Add(filteredSkill);
                }
            }

            return availableSkills;
        }

        public List<Skill> GetAvailableSkills(SkillFilterType skillFilterType)
        {
            return GetAvailableSkills(currentBranch, skillFilterType);
        }

        public void GetPathSkills(SkillBranchMapping skillBranchMapping, ref List<Skill> pathSkills, SkillBranch skillBranch = null)
        {
            if (pathSkills == null) { return; }
            if (!currentBranch.HasBranch(skillBranchMapping)) { return; }

            if (skillBranch == null) { skillBranch = skillTree.GetSkillBranchFromID(currentBranch.GetBranch(skillBranchMapping)); }
            pathSkills.AddRange(skillBranch.GetAllSkills());
            foreach (SkillBranchMapping subBranchMapping in GetAvailableBranchMappings(skillBranch))
            {
                SkillBranch subBranch = skillTree.GetSkillBranchFromID(skillBranch.GetBranch(subBranchMapping));
                pathSkills.AddRange(subBranch.GetAllSkills());
                GetPathSkills(subBranchMapping, ref pathSkills, subBranch);
            }
        }

        public List<SkillBranchMapping> GetAvailableBranchMappings()
        {
            if (currentBranch == null) { ResetCurrentBranch(); }
            return GetAvailableBranchMappings(currentBranch);
        }

        private List<SkillBranchMapping> GetAvailableBranchMappings(SkillBranch skillBranch)
        {
            List<SkillBranchMapping> availableBranches = new List<SkillBranchMapping>();
            if (skillBranch.HasBranch(SkillBranchMapping.up)) { availableBranches.Add(SkillBranchMapping.up); }
            if (skillBranch.HasBranch(SkillBranchMapping.left)) { availableBranches.Add(SkillBranchMapping.left); }
            if (skillBranch.HasBranch(SkillBranchMapping.right)) { availableBranches.Add(SkillBranchMapping.right); }
            if (skillBranch.HasBranch(SkillBranchMapping.down)) { availableBranches.Add(SkillBranchMapping.down); }

            return availableBranches;
        }

        private Skill FilterSkill(Skill skill, SkillFilterType skillFilterType, int levelForEvaluation = -1)
        {
            Skill filteredSkill = null;
            if (skillFilterType == SkillFilterType.All || skillFilterType == SkillFilterType.Trait)
            {
                filteredSkill = FilterSkillByTrait(skill, levelForEvaluation);
            }
            if (skillFilterType == SkillFilterType.All || skillFilterType == SkillFilterType.AP)
            {
                filteredSkill = FilterSkillByRemainingAP(filteredSkill);
            }
            return filteredSkill;
        }

        private Skill FilterSkillByTrait(Skill skill, int levelForEvaluation = -1)
        {
            if (skill == null) { return null; }
            if (levelForEvaluation == -1) { levelForEvaluation = skillTreeLevel; }

            SkillStat skillStat = skill.GetStat();

            if (Enum.TryParse(skillStat.ToString(), out Stat stat))
            {
                float value = baseStats.GetStat(stat);
                return (value >= levelForEvaluation * skillTreeLevelMultiplierForStatUnlock) ? skill : null;
            }
            return null;
        }

        private Skill FilterSkillByRemainingAP(Skill skill)
        {
            if (skill == null) { return null; }

            float remainingAP = combatParticipant.GetAP();
            if (remainingAP >= skill.GetAPCost())
            {
                return skill;
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
