using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    [RequireComponent(typeof(SkillHandler))]
    public class BattleAI : MonoBehaviour
    {
        // Tunables
        [SerializeField] [Range(0, 1)] float probabilityToTraverseSkillTree = 0.8f;

        // Cached References
        CombatParticipant combatParticipant = null;
        SkillHandler skillHandler = null;
        BattleController battleController = null;

        private void Awake()
        {
            combatParticipant = GetComponent<CombatParticipant>();
            skillHandler = GetComponent<SkillHandler>();
        }

        private void OnEnable()
        {
            combatParticipant.enterCombat += UpdateBattleController;
        }

        private void OnDisable()
        {
            combatParticipant.enterCombat -= UpdateBattleController;
        }

        private void Update()
        {
            if (battleController == null) { return; }
            QueueNextAction();
        }

        private void UpdateBattleController(bool active)
        {
            if (active) { battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>(); }
            else { battleController = null; }
        }

        public void QueueNextAction()
        {
            if (battleController.GetBattleState() == BattleState.Combat 
                && !combatParticipant.IsDead()
                && combatParticipant.IsInCombat()
                && !combatParticipant.IsInCooldown())
            {
                Skill skill = GetSkill();
                List<CombatParticipant> targets;

                // Randomize input combat participants selections
                List<CombatParticipant> characters = new List<CombatParticipant>(battleController.GetCharacters()); // Local copy since shuffling done in place
                characters.Shuffle();
                List<CombatParticipant> enemies = new List<CombatParticipant>(battleController.GetEnemies()); // Local copy since shuffling done in place
                enemies.Shuffle();

                BattleActionData battleActionData = new BattleActionData(combatParticipant);
                if (combatParticipant.GetFriendly())
                {
                    skill.GetTargets(true, battleActionData, characters, enemies);
                }
                else
                {
                    skill.GetTargets(true, battleActionData, enemies, characters);
                }
                if (battleActionData.targetCount == 0) { return; }
                if (combatParticipant == null || skill == null) { return; }

                battleController.AddToBattleQueue(battleActionData, skill);
                ClearSelectionMemory();
            }
        }

        public virtual Skill GetSkill()
        {
            // Simple implementation -- choose at random
            List<SkillBranchMapping> availableBranches = skillHandler.GetAvailableBranchMappings();
            int branchCount = availableBranches.Count;

            if (branchCount > 0)
            {
                int branchIndex = Random.Range(0, branchCount);

                float traverseChance = Random.Range(0f, 1f);
                if (probabilityToTraverseSkillTree >= traverseChance)
                {
                    // Walk to next branch, recurse through tree
                    skillHandler.SetBranch(availableBranches[branchIndex], SkillFilterType.None);
                    return GetSkill();
                }
            }

            // Otherwise just select skill from existing options
            List<Skill> skillOptions = skillHandler.GetUnfilteredSkills();
            int skillCount = skillOptions.Count;
            if (skillCount == 0) { return null; }

            int skillIndex = Random.Range(0, skillCount);
            return skillOptions[skillIndex];
        }

        private void ClearSelectionMemory()
        {
            skillHandler.ResetCurrentBranch();
        }
    }
}
