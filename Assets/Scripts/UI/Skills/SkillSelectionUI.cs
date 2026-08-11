using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Stats;
using UnityEngine.Localization.Tables;

namespace Frankie.Combat.UI
{
    public class SkillSelectionUI : UIBox<AbilitiesBoxState>, ILocalizable
    {
        // Tunables
        [Header("Skill Selection Text")]
        [SerializeField] protected string defaultNoText = "--";
        [Header("Skill Selection Hookups")]
        [SerializeField] private TMP_Text selectedCharacterNameField;
        [SerializeField] protected TMP_Text skillField;
        [SerializeField] private UIChoice upField;
        [SerializeField] private UIChoice leftField;
        [SerializeField] private UIChoice rightField;
        [SerializeField] private UIChoice downField;

        [Header("Configuration")] 
        [SerializeField] private Color noSkillColor = Color.gray;
        [SerializeField] private Color selectedSkillColor = Color.softYellow;
        
        // State
        private bool usingBattleController = false;
        protected CombatParticipant selectedCharacter;

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

        protected override void AwakeTriggered()
        {
            handleGlobalInput = false;
        }

        protected override void StartTriggered()
        {
            if (skillField != null)
            {
                skillField.color = noSkillColor;
                skillField.SetText(defaultNoText);
            }
        }

        protected override void EnableTriggered()
        {
            if (usingBattleController)
            {
                BattleEventBus<BattleEntitySelectedEvent>.SubscribeToEvent(HandleBattleEntitySelectedEvent);
                battleController.SubscribeToBattleInput(true, HandleInput);
            }
        }

        protected override void DisableTriggered()
        {
            if (usingBattleController)
            {
                BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(HandleBattleEntitySelectedEvent);
                battleController.SubscribeToBattleInput(false, HandleInput);
            }
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public virtual List<TableEntryReference> GetLocalizationEntries() => new();
        #endregion

        #region InputHandlers
        protected virtual void HandleInput(ControllerInputType input)
        {
            if (selectedCharacter == null) {return; }
            if (battleController.IsBattleActionArmed()) { return; } // Need to manually check because can be armed while UI element disabled (InventoryBox-based)
            SetBranchOrSkill(selectedCharacter, input);
        }

        public void HandleInput(int input) // PUBLIC:  Called via unity events for button clicks (mouse)
        {
            // Because Unity hates handling enums
            var battleInputType = (ControllerInputType)input;
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
        protected virtual void PassSkillFlavour(Stat skillStat, string detail, float apCost)
        {
            // Null implementation, for parsing in alternate context
        }

        protected virtual void ResetUI()
        {
            ResetUI(true, true);
        }
        
        protected void ResetUI(bool resetAllFields, bool resetAlpha)
        {
            skillField.color = noSkillColor;
            if (resetAllFields) { ResetAllFields(); }
            if (resetAlpha) { canvasGroup.alpha = 0; }
        }
        
        protected void RefreshUI(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> battleEntities)
        {
            if (combatParticipantType != CombatParticipantType.Friendly) { return; }
            if (battleController != null)
            {
                // Do not pop skill selection if using an item
                if (battleController.GetActiveBattleAction() != null && battleController.GetActiveBattleAction().IsItem()) { return; } 
            }

            selectedCharacter = battleEntities.First().combatParticipant; // Expectation is single entry, handling edge case
            if (selectedCharacter == null) { ResetUI(); return; }

            UpdateSkillHandler();
        }
        
        protected void UpdateSkillHandler()
        {
            if (selectedCharacter == null) { return; }

            canvasGroup.alpha = 1;
            selectedCharacterNameField.SetText(selectedCharacter.GetCombatName());
            var skillHandler = selectedCharacter.GetComponent<SkillHandler>();
            skillHandler.ResetCurrentBranch();
            UpdateSkills(skillHandler);
        }
        
        protected bool SetBranchOrSkill(CombatParticipant combatParticipant, ControllerInputType input)
        {
            if (combatParticipant == null) { return false; }

            bool validInput = false;
            SkillBranchMapping skillBranchMapping = default;
            switch (input)
            {
                case ControllerInputType.NavigateUp:
                    skillBranchMapping = SkillBranchMapping.Up; validInput = true;
                    break;
                case ControllerInputType.NavigateLeft:
                    skillBranchMapping = SkillBranchMapping.Left; validInput = true;
                    break;
                case ControllerInputType.NavigateRight:
                    skillBranchMapping = SkillBranchMapping.Right; validInput = true;
                    break;
                case ControllerInputType.NavigateDown:
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
        private void ResetAllFields()
        {
            selectedCharacterNameField.SetText(defaultNoText);
            upField.SetText(defaultNoText);
            leftField.SetText(defaultNoText);
            rightField.SetText(defaultNoText);
            downField.SetText(defaultNoText);
            skillField.SetText(defaultNoText);
        }
        
        private void UpdateSkills(SkillHandler skillHandler)
        {
            skillHandler.GetPlayerSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down);
            upField.SetText(up != null ? up.GetName() : defaultNoText);
            leftField.SetText(left != null ? left.GetName() : defaultNoText);
            rightField.SetText(right != null ? right.GetName() : defaultNoText);
            downField.SetText(down != null ? down.GetName() : defaultNoText);

            Skill activeSkill = skillHandler.GetActiveSkill();
            if (activeSkill != null)
            {
                skillField.color = selectedSkillColor;
                skillField.SetText(activeSkill.GetName());
                if (battleController != null) { battleController.SetActiveBattleAction(activeSkill); }
                TriggerUIBoxModified(ReceiverModifiedType.ItemSelected, new ReceiverModifiedData(this));
            }
            else
            {
                skillField.color = noSkillColor;
                skillField.SetText(defaultNoText);
            }
        }
        #endregion
    }
}
