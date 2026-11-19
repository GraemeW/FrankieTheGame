using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.Utils;
using Frankie.Stats;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    [RequireComponent(typeof(BaseStats))]
    public class SkillHandler : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("SkillTree or SkillPool")] private SkillSet skillSet;
        [SerializeField][Min(0)] private float skillTreeLevelMultiplierForStatUnlock = 10f;

        // Editor Field References
        internal const string skillSetField = nameof(skillSet);
        internal const string skillTreeLevelMultiplierForStatUnlockField = nameof(skillTreeLevelMultiplierForStatUnlock);

        // Const Tunables
        internal const int poolSlotCount = 4; // Must match SkillBranchMapping (contiguous 0..3)

        // State
        private bool isSelectionInitialized = false;
            // Tree
        private SkillBranch currentBranch;
        private Skill activeSkill;
        private int skillTreeLevel = 0;
            // Pool
        private readonly Skill[] poolSlots = new Skill[poolSlotCount];
        private bool isPoolRerollPending = true;

        // Cached References
        private CombatParticipant combatParticipant;
        private BaseStats baseStats;

        // Events
        public event Action skillSelectionChanged;

        #region UnityMethods
        private void Awake()
        {
            combatParticipant = GetComponent<CombatParticipant>();
            baseStats = GetComponent<BaseStats>();
        }

        private void OnEnable()
        {
            combatParticipant.enteredBattle += SubscribeToBattle;
        }

        private void OnDisable()
        {
            combatParticipant.enteredBattle -= SubscribeToBattle;
            SubscribeToBattle(false);
        }
        #endregion

        #region StandardMethods
        public bool HasSkillSet() => skillSet != null;
        public Skill GetActiveSkill() => activeSkill;

        public void GetSkillsForCurrentSelection(out Skill up, out Skill left, out Skill right, out Skill down)
        {
            InitializeSelectionIfNeeded();

            up = FilterSkill(GetMappedSkill(SkillBranchMapping.Up), SkillFilterType.All);
            left = FilterSkill(GetMappedSkill(SkillBranchMapping.Left), SkillFilterType.All);
            right = FilterSkill(GetMappedSkill(SkillBranchMapping.Right), SkillFilterType.All);
            down = FilterSkill(GetMappedSkill(SkillBranchMapping.Down), SkillFilterType.All);
        }

        public void SetBranchOrSkill(SkillBranchMapping skillBranchMapping, SkillFilterType skillFilterType)
        {
            // Attempt to set branch first, otherwise set skill
            if (!TrySetBranch(skillBranchMapping, skillFilterType)) { SetSkill(skillBranchMapping); }
            skillSelectionChanged?.Invoke();
        }

        public bool TryGetRandomSkill(ICollection<Skill> skillsToExclude, bool filterByAP, out Skill skill) => TryGetRandomSkill(skillsToExclude, filterByAP, null, out skill);
        public bool TryGetRandomSkill(ICollection<Skill> skillsToExclude, bool filterByAP, ICollection<Skill> skillsToChooseFrom, out Skill skill)
        {
            return GetWeightedCandidates(skillsToExclude, filterByAP, skillsToChooseFrom).TryGetRandom(out skill);
        }

        public void ResetSelection()
        {
            InitializeSelection();
            skillSelectionChanged?.Invoke();
        }
        #endregion

        #region SelectionMethods
        private SkillTree GetSkillTree() => skillSet as SkillTree;

        private Skill GetMappedSkill(SkillBranchMapping skillBranchMapping)
        {
            if (!isSelectionInitialized) { return null; }

            SkillTree skillTree = GetSkillTree();
            if (skillTree != null) { return currentBranch != null ? currentBranch.GetSkill(skillBranchMapping) : null; }
            return poolSlots[(int)skillBranchMapping]; // Pool-based fallback, simply maps directional enum -> pool index (non-POR path)
        }

        private bool TrySetBranch(SkillBranchMapping skillBranchMapping, SkillFilterType skillFilterType)
        {
            if (!isSelectionInitialized) { InitializeSelection(); return true; }

            SkillTree skillTree = GetSkillTree();
            if (skillTree == null || currentBranch == null) { return false; }
            if (!currentBranch.HasBranch(skillBranchMapping)) { return false; }

            // Check if available skills exist after filtering
            Skill tryActiveSkill = FilterSkill(currentBranch.GetSkill(skillBranchMapping), SkillFilterType.All);
            if (tryActiveSkill == null) { return false; }

            // Check if meaningful to traverse branch after filtering
            activeSkill = tryActiveSkill;
            SkillBranch tryCurrentBranch = skillTree.GetSkillBranchFromID(currentBranch.GetBranch(skillBranchMapping));
            if (tryCurrentBranch == null) { return false; }
            int availableSkillCount = GetAvailableSkills(tryCurrentBranch, skillFilterType, skillTreeLevel + 1).Count;
            if (availableSkillCount == 0) { return false; }

            currentBranch = tryCurrentBranch;
            skillTreeLevel++;
            return true;
        }

        private bool SetSkill(SkillBranchMapping skillBranchMapping)
        {
            Skill tryActiveSkill = FilterSkill(GetMappedSkill(skillBranchMapping), SkillFilterType.All);
            if (tryActiveSkill == null) { return false; }

            activeSkill = tryActiveSkill;
            return true;
        }

        private void InitializeSelectionIfNeeded()
        {
            if (!isSelectionInitialized) { InitializeSelection(); }
        }
        
        private void InitializeSelection() // Note:  Silent (no change event) - also used for lazy setup from getters
        {
            if (skillSet == null) { return; }

            // Init Skill Tree Path
            SkillTree skillTree = GetSkillTree();
            currentBranch = skillTree != null ? skillTree.GetRootSkillBranch() : null;
            // Init Skill Pool Path
            if (skillTree == null && isPoolRerollPending) { RollPoolSlots(); }

            activeSkill = null;
            skillTreeLevel = 0;
            isSelectionInitialized = true;
        }

        private void RollPoolSlots()
        {
            Array.Clear(poolSlots, 0, poolSlots.Length);
            GetWeightedCandidates(null, true, null).TryGetRandom(poolSlots.Length, out List<Skill> rolledSkills);
            rolledSkills.CopyTo(poolSlots);
            isPoolRerollPending = false;
        }
        #endregion

        #region BattleEvents
        private void SubscribeToBattle() { SubscribeToBattle(true); }
        private void SubscribeToBattle(bool enable)
        {
            if (enable)
            {
                if (skillSet == null || GetSkillTree() != null) { return; } // Rerolls only matter for pools, trees keep a fixed selection
                isPoolRerollPending = true;
                BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
                BattleEventBus<BattleQueueUpdatedEvent>.SubscribeToEvent(HandleBattleQueueUpdatedEvent);
            }
            else
            {
                BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent);
                BattleEventBus<BattleQueueUpdatedEvent>.UnsubscribeFromEvent(HandleBattleQueueUpdatedEvent);
            }
        }

        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            switch (battleStateChangedEvent.battleState)
            {
                case BattleState.Outro:
                case BattleState.Complete when battleStateChangedEvent.battleOutcome == BattleOutcome.Aborted: // No outro on an aborted battle
                    SubscribeToBattle(false);
                    break;
            }
        }

        private void HandleBattleQueueUpdatedEvent(BattleQueueUpdatedEvent battleQueueUpdatedEvent)
        {
            BattleActionData battleActionData = battleQueueUpdatedEvent.battleSequence.battleActionData;
            if (battleActionData == null || battleActionData.GetSender() != combatParticipant) { return; }
            
            isPoolRerollPending = true; // i.e. only picked up on the next selection reset (a simple cancel and reselect keeps the same slots ~ cannot fish)
        }
        #endregion

        #region UtilityMethods
        private IEnumerable<IObjectProbabilityPair<Skill>> GetWeightedCandidates(ICollection<Skill> skillsToExclude, bool filterByAP, ICollection<Skill> skillsToChooseFrom)
        {
            return skillSet != null ? skillSet.GetWeightedSkills(skillsToExclude, skillsToChooseFrom, filterByAP ? combatParticipant.GetAP() : null) : Enumerable.Empty<IObjectProbabilityPair<Skill>>();
        }

        private List<Skill> GetAvailableSkills(SkillBranch skillBranch, SkillFilterType skillFilterType, int filterLevel = -1)
        {
            return skillBranch.GetAllSkills().Select(skill => FilterSkill(skill, skillFilterType, filterLevel)).Where(filteredSkill => filteredSkill != null).ToList();
        }

        private Skill FilterSkill(Skill skill, SkillFilterType skillFilterType, int levelForEvaluation = -1)
        {
            Skill filteredSkill = null;
            if (skillFilterType is SkillFilterType.All or SkillFilterType.Trait)
            {
                filteredSkill = FilterSkillByTrait(skill, levelForEvaluation);
            }
            if (skillFilterType is SkillFilterType.All or SkillFilterType.AP)
            {
                filteredSkill = FilterSkillByRemainingAP(filteredSkill);
            }
            return filteredSkill;
        }
        
        private Skill FilterSkillByRemainingAP(Skill skill) => skill != null && CanAfford(skill) ? skill : null;
        
        private bool CanAfford(Skill skill) => combatParticipant.GetAP() >= skill.GetAPCost();

        private Skill FilterSkillByTrait(Skill skill, int levelForEvaluation = -1)
        {
            if (skill == null) { return null; }
            if (levelForEvaluation == -1) { levelForEvaluation = skillTreeLevel; }

            Stat skillStat = skill.GetStat();
            float value = baseStats.GetStat(skillStat);
            return (value >= levelForEvaluation * skillTreeLevelMultiplierForStatUnlock) ? skill : null;
        }
        #endregion
    }
}
