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
        
        #region UnityMethods
        protected override void OnEnable()
        {
            if (!usingBattleController) { base.OnEnable(); }
            else
            {
                BattleEventBus<BattleEntitySelectedEvent>.SubscribeToEvent(HandleBattleEntitySelectedEvent);
                battleController.battleInput += HandleInput;
            }
        }

        protected override void OnDisable()
        {
            if (!usingBattleController) { base.OnDisable(); }
            else
            {
                BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(HandleBattleEntitySelectedEvent);
                battleController.battleInput -= HandleInput;
            }
        }
        #endregion

        #region InputHandlers
        protected virtual void HandleInput(PlayerInputType input)
        {
            if (currentCombatParticipant == null) {return; }
            if (battleController.IsBattleActionArmed()) { return; } // Need to manually check because can be armed while UI element disabled (InventoryBox-based)
            SetBranchOrSkill(currentCombatParticipant, input);
        }

        public void HandleInput(int input) // PUBLIC:  Called via unity events for button clicks (mouse)
        {
            // Because Unity hates handling enums
            var battleInputType = (PlayerInputType)input;
            HandleInput(battleInputType);
        }
        #endregion

        #region EventHandlers
        private void HandleBattleEntitySelectedEvent(BattleEntitySelectedEvent battleEntitySelectedEvent)
        {
            RefreshUI(battleEntitySelectedEvent.combatParticipantType, battleEntitySelectedEvent.battleEntities);
        }
        #endregion

        #region ProtectedMethods
        protected virtual void PassSkillFlavour(SkillStat skillStat, string detail, float apCost)
        {
            // Null implementation, for parsing in alternate context
        }

        protected virtual void ResetUI()
        {
            SetAllFields(defaultNoText);
            canvasGroup.alpha = 0;
        }
        
        protected void RefreshUI(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> battleEntities)
        {
            if (combatParticipantType != CombatParticipantType.Friendly) { return; }
            if (battleController != null)
            {
                // Do not pop skill selection if using an item
                if (battleController.GetActiveBattleAction() != null && battleController.GetActiveBattleAction().IsItem()) { return; } 
            }

            currentCombatParticipant = battleEntities.First().combatParticipant; // Expectation is single entry, handling edge case
            if (currentCombatParticipant == null) { ResetUI(); return; }

            UpdateSkillHandler();
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
        
        protected bool SetBranchOrSkill(CombatParticipant combatParticipant, PlayerInputType input)
        {
            if (combatParticipant == null) { return false; }

            bool validInput = false;
            SkillBranchMapping skillBranchMapping = default;
            switch (input)
            {
                case PlayerInputType.NavigateUp:
                    skillBranchMapping = SkillBranchMapping.Up; validInput = true;
                    break;
                case PlayerInputType.NavigateLeft:
                    skillBranchMapping = SkillBranchMapping.Left; validInput = true;
                    break;
                case PlayerInputType.NavigateRight:
                    skillBranchMapping = SkillBranchMapping.Right; validInput = true;
                    break;
                case PlayerInputType.NavigateDown:
                    skillBranchMapping = SkillBranchMapping.Down; validInput = true;
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

        #region PrivateUtility
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
                OnUIBoxModified(UIBoxModifiedType.ItemSelected, true);
            } 
            else { skillField.text = defaultNoText; }
        }
        #endregion
    }
}
