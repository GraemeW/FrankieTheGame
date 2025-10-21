using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Frankie.Control;
using Frankie.Utils.UI;

namespace Frankie.Combat.UI
{
    public class SkillSelectionUI : UIBox
    {
        // Tunables
        [SerializeField] private TextMeshProUGUI selectedCharacterNameField;
        [SerializeField] private TextMeshProUGUI upField;
        [SerializeField] private TextMeshProUGUI leftField;
        [SerializeField] private TextMeshProUGUI rightField;
        [SerializeField] private TextMeshProUGUI downField;
        [SerializeField] protected TextMeshProUGUI skillField;
        [SerializeField] protected string defaultNoText = "--";

        // State
        private bool usingBattleController = false;
        protected CombatParticipant currentCombatParticipant;

        // Cached References
        private BattleController battleController;

        #region PublicMethods
        public void SetupBattleController(BattleController setBattleController)
        {
            battleController = setBattleController;
            controller = battleController;
            usingBattleController = true;
        }
        #endregion
        
        #region VirtualMethods
        protected override void OnEnable()
        {
            if (!usingBattleController) { base.OnEnable(); }
            else
            {
                BattleEventBus<BattleEntitySelectedEvent>.SubscribeToEvent(RefreshUI);
                battleController.battleInput += HandleInput;
            }
        }

        protected override void OnDisable()
        {
            if (!usingBattleController) { base.OnDisable(); }
            else
            {
                BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(RefreshUI);
                battleController.battleInput -= HandleInput;
            }
        }

        protected virtual void HandleInput(PlayerInputType input)
        {
            if (battleController.GetSelectedCharacter() == null) { return; }
            if (battleController.IsBattleActionArmed()) { return; }
            if (SetBranchOrSkill(input)) { return; }
        }

        public void HandleInput(int input) // PUBLIC:  Called via unity events for button clicks (mouse)
        {
            // Because Unity hates handling enums
            var battleInputType = (PlayerInputType)input;
            HandleInput(battleInputType);
        }

        protected virtual void PassSkillFlavour(SkillStat skillStat, string detail, float apCost)
        {
            // Null implementation, for parsing in alternate context
        }

        protected virtual void ResetUI()
        {
            SetAllFields(defaultNoText);
            canvasGroup.alpha = 0;
        }
        #endregion

        #region PrivateMethods
        protected void RefreshUI(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> battleEntities)
        {
            if (combatParticipantType != CombatParticipantType.Friendly) { return; }
            if (battleController != null)
            {
                // Do not pop skill selection if using an item
                if (battleController.GetActiveBattleAction() != null && battleController.GetActiveBattleAction().IsItem()) { return; } 
            }
            if (battleEntities == null) { ResetUI(); return; }

            currentCombatParticipant = battleEntities.First().combatParticipant; // Expectation is single entry, handling edge case
            if (currentCombatParticipant == null) { ResetUI(); return; }

            UpdateSkillHandler();
        }

        private void RefreshUI(BattleEntitySelectedEvent battleEntitySelectedEvent)
        {
            RefreshUI(battleEntitySelectedEvent.combatParticipantType, battleEntitySelectedEvent.battleEntities);
        }

        protected void UpdateSkillHandler()
        {
            if (currentCombatParticipant == null) { return; }

            canvasGroup.alpha = 1;
            selectedCharacterNameField.text = currentCombatParticipant.GetCombatName();
            var skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            skillHandler.ResetCurrentBranch();
            UpdateSkills(skillHandler);
        }

        protected void SetAllFields(string text)
        {
            selectedCharacterNameField.text = text;
            upField.text = text;
            leftField.text = text;
            rightField.text = text;
            downField.text = text;
            skillField.text = text;
        }

        private void UpdateSkills(SkillHandler skillHandler)
        {
            skillHandler.GetPlayerSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down);
            upField.text = up != null ? Skill.GetSkillNamePretty(up.name) : defaultNoText;
            leftField.text = left != null ? Skill.GetSkillNamePretty(left.name) : defaultNoText;
            rightField.text = right != null ? Skill.GetSkillNamePretty(right.name) : defaultNoText;
            downField.text = down != null ? Skill.GetSkillNamePretty(down.name) : defaultNoText;

            Skill activeSkill = skillHandler.GetActiveSkill();
            if (activeSkill != null)
            { 
                skillField.text = Skill.GetSkillNamePretty(activeSkill.name);
                if (battleController != null) { battleController.SetActiveBattleAction(activeSkill); }
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);
            } 
            else { skillField.text = defaultNoText; }
        }

        protected bool SetBranchOrSkill(PlayerInputType input)
        {
            currentCombatParticipant = battleController.GetSelectedCharacter();
            return SetBranchOrSkill(currentCombatParticipant, input);
        }

        protected bool SetBranchOrSkill(CombatParticipant combatParticipant, PlayerInputType input)
        {
            if (combatParticipant == null) { return false; }

            bool validInput = false;
            SkillBranchMapping skillBranchMapping = default;
            switch (input)
            {
                case PlayerInputType.NavigateUp:
                    skillBranchMapping = SkillBranchMapping.up; validInput = true;
                    break;
                case PlayerInputType.NavigateLeft:
                    skillBranchMapping = SkillBranchMapping.left; validInput = true;
                    break;
                case PlayerInputType.NavigateRight:
                    skillBranchMapping = SkillBranchMapping.right; validInput = true;
                    break;
                case PlayerInputType.NavigateDown:
                    skillBranchMapping = SkillBranchMapping.down; validInput = true;
                    break;
            }
            if (!validInput) return false;
            
            var skillHandler = combatParticipant.GetComponent<SkillHandler>();
            skillHandler.SetBranchOrSkill(skillBranchMapping, SkillFilterType.All);
            UpdateSkills(skillHandler);
            Skill activeSkill = skillHandler.GetActiveSkill();
            if (activeSkill != null)
            {
                PassSkillFlavour(activeSkill.GetStat(), activeSkill.GetDetail(), activeSkill.GetAPCost());
            }
            return true;
        }

        protected static void ResetSkillHandler(CombatParticipant combatParticipant)
        {
            var skillHandler = combatParticipant.GetComponent<SkillHandler>();
            skillHandler.ResetCurrentBranch();
        }
        #endregion
    }
}
