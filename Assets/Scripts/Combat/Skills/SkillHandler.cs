using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    [RequireComponent(typeof(BaseStats))]
    public class SkillHandler : MonoBehaviour
    {
        // Tunables
        [SerializeField] private SkillTree skillTree;
        [SerializeField][Min(0)] private float skillTreeLevelMultiplierForStatUnlock = 10f;

        // State
        private SkillBranch currentBranch;
        private Skill activeSkill;
        private int skillTreeLevel = 0;

        // Cached References
        private CombatParticipant combatParticipant;
        private BaseStats baseStats;

        #region UnityMethods
        private void Awake()
        {
            combatParticipant = GetComponent<CombatParticipant>();
            baseStats = GetComponent<BaseStats>();
        }
        #endregion

        #region StandardMethods
        public bool HasSkillTree() => skillTree != null;
        public Skill GetActiveSkill() => activeSkill;

        public void GetPlayerSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down)
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            up = FilterSkill(currentBranch.GetSkill(SkillBranchMapping.Up), SkillFilterType.All);
            left = FilterSkill(currentBranch.GetSkill(SkillBranchMapping.Left), SkillFilterType.All);
            right = FilterSkill(currentBranch.GetSkill(SkillBranchMapping.Right), SkillFilterType.All);
            down = FilterSkill(currentBranch.GetSkill(SkillBranchMapping.Down), SkillFilterType.All);
        }

        public void SetBranchOrSkill(SkillBranchMapping skillBranchMapping, SkillFilterType skillFilterType)
        {
            // Attempt to set branch first, otherwise set skill
            if (SetBranch(skillBranchMapping, skillFilterType)) { return; }
            if (SetSkill(skillBranchMapping)) { return; }
        }

        public bool SetBranch(SkillBranchMapping skillBranchMapping, SkillFilterType skillFilterType)
        {
            if (currentBranch == null) { ResetCurrentBranch(); return true; }
            if (!currentBranch.HasBranch(skillBranchMapping)) { return false; }
            
            // Check if available skills exist after filtering
            Skill tryActiveSkill = FilterSkill(currentBranch.GetSkill(skillBranchMapping), SkillFilterType.All);
            if (tryActiveSkill == null) { return false; }
            
            // Check if meaningful to traverse branch after filtering
            activeSkill = tryActiveSkill;
            SkillBranch tryCurrentBranch = skillTree.GetSkillBranchFromID(currentBranch.GetBranch(skillBranchMapping));
            int availableSkillCount = GetAvailableSkills(tryCurrentBranch, skillFilterType, skillTreeLevel + 1).Count;
            if (availableSkillCount == 0) { return false; }

            currentBranch = tryCurrentBranch;
            skillTreeLevel++;
            return true;
        }

        private bool SetSkill(SkillBranchMapping skillBranchMapping)
        {
            if (!currentBranch.HasSkill(skillBranchMapping)) { return false; }
            
            Skill tryActiveSkill = FilterSkill(currentBranch.GetSkill(skillBranchMapping), SkillFilterType.All);
            if (tryActiveSkill == null){ return false; }

            activeSkill = tryActiveSkill; 
            return true;
        }
        #endregion

        #region UtilityMethods
        public List<Skill> GetUnfilteredSkills() => GetUnfilteredSkills(currentBranch);
        private List<Skill> GetUnfilteredSkills(SkillBranch skillBranch)
        {
            if (currentBranch == null) { ResetCurrentBranch(); }
            return skillBranch.GetAllSkills().Where(skill => skill != null).ToList();
        }

        public List<Skill> GetAvailableSkills(SkillFilterType skillFilterType) => GetAvailableSkills(currentBranch, skillFilterType);
        private List<Skill> GetAvailableSkills(SkillBranch skillBranch, SkillFilterType skillFilterType, int filterLevel = -1)
        {
            return skillBranch.GetAllSkills().Select(skill => FilterSkill(skill, skillFilterType, filterLevel)).Where(filteredSkill => filteredSkill != null).ToList();
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
            var availableBranches = new List<SkillBranchMapping>();
            if (skillBranch.HasBranch(SkillBranchMapping.Up)) { availableBranches.Add(SkillBranchMapping.Up); }
            if (skillBranch.HasBranch(SkillBranchMapping.Left)) { availableBranches.Add(SkillBranchMapping.Left); }
            if (skillBranch.HasBranch(SkillBranchMapping.Right)) { availableBranches.Add(SkillBranchMapping.Right); }
            if (skillBranch.HasBranch(SkillBranchMapping.Down)) { availableBranches.Add(SkillBranchMapping.Down); }

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
            if (!Enum.TryParse(skillStat.ToString(), out Stat stat)) { return null; }
            
            float value = baseStats.GetStat(stat);
            return (value >= levelForEvaluation * skillTreeLevelMultiplierForStatUnlock) ? skill : null;
        }

        private Skill FilterSkillByRemainingAP(Skill skill)
        {
            if (skill == null) { return null; }

            float remainingAP = combatParticipant.GetAP();
            return remainingAP >= skill.GetAPCost() ? skill : null;
        }

        public void ResetCurrentBranch()
        {
            if (skillTree == null) { return; }

            currentBranch = skillTree.GetRootSkillBranch();
            activeSkill = null;
            skillTreeLevel = 0;
        }
        #endregion
    }
}
