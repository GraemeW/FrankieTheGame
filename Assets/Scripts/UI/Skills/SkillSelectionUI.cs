using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Tables;
using TMPro;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using Frankie.Stats;
using Frankie.Utils.Localization;


namespace Frankie.Combat.UI
{
    public class SkillSelectionUI : UIBox<AbilitiesBoxState>, ILocalizable
    {
        // Tunables
        [Header("Skill Selection Text")]
        [SerializeField] protected string defaultNoText = "--";
        [Header("Skill Selection Hookups")]
        [SerializeField] private TMP_Text selectedCharacterNameField;
        [SerializeField] private TMP_Text skillField;
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
        private SkillSelectionModel skillSelectionModel;

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
            skillSelectionModel = CreateSkillSelectionModel();
            if (TryGetComponent(out UIToolkitMenuView menuView)) // TODO:  Should be able to remove TryGet and just generalize for both Abilities + SkillSelectionUI once uGUI removed from battle components
            {
                menuView.SetDataSource(skillSelectionModel);
                menuView.AddEntry(new SkillWheelHandle(menuView, skillSelectionModel, HandleInput));
            }
        }

        protected override void StartTriggered()
        {
            SetActiveSkill(null);
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

        #region ModelMethods
        protected virtual SkillSelectionModel CreateSkillSelectionModel() => new();

        // Transitional:  legacy uGUI fields mirror the model (battle SkillSelection prefab) - remove with the uGUI path
        private void RefreshLegacyFields()
        {
            if (TryGetBoxView(out IUIBoxView _)) { return; }

            selectedCharacterNameField.SetText(skillSelectionModel.characterName);
            upField.SetText(skillSelectionModel.upSkillText);
            leftField.SetText(skillSelectionModel.leftSkillText);
            rightField.SetText(skillSelectionModel.rightSkillText);
            downField.SetText(skillSelectionModel.downSkillText);
            skillField.SetText(skillSelectionModel.activeSkillText);
            skillField.color = skillSelectionModel.hasActiveSkill ? selectedSkillColor : noSkillColor;
        }

        private void SetActiveSkill(Skill activeSkill)
        {
            skillSelectionModel.hasActiveSkill = activeSkill != null;
            skillSelectionModel.activeSkillText = activeSkill != null ? activeSkill.GetName() : defaultNoText;
            RefreshLegacyFields();
        }
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
            RefreshUI(battleEntitySelectedEvent.selectionType, battleEntitySelectedEvent.battleEntities);
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
            skillSelectionModel.hasActiveSkill = false;
            if (resetAllFields) { ResetAllFields(); }
            RefreshLegacyFields();
            if (resetAlpha) { SetVisible(false); }
        }

        protected void ClearActiveSkill() => SetActiveSkill(null);
        
        protected void RefreshUI(BattleEntitySelectionType selectionType, IEnumerable<BattleEntity> battleEntities)
        {
            if (selectionType != BattleEntitySelectionType.Actor) { return; }
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

            SetVisible(true);
            skillSelectionModel.characterName = selectedCharacter.GetCombatName();
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
            skillSelectionModel.characterName = defaultNoText;
            skillSelectionModel.upSkillText = defaultNoText;
            skillSelectionModel.leftSkillText = defaultNoText;
            skillSelectionModel.rightSkillText = defaultNoText;
            skillSelectionModel.downSkillText = defaultNoText;
            skillSelectionModel.activeSkillText = defaultNoText;
        }
        
        private void UpdateSkills(SkillHandler skillHandler)
        {
            skillHandler.GetPlayerSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down);
            skillSelectionModel.upSkillText = up != null ? up.GetName() : defaultNoText;
            skillSelectionModel.leftSkillText = left != null ? left.GetName() : defaultNoText;
            skillSelectionModel.rightSkillText = right != null ? right.GetName() : defaultNoText;
            skillSelectionModel.downSkillText = down != null ? down.GetName() : defaultNoText;

            Skill activeSkill = skillHandler.GetActiveSkill();
            SetActiveSkill(activeSkill);
            if (activeSkill == null) { return; }

            if (battleController != null) { battleController.SetActiveBattleAction(activeSkill); }
            TriggerUIBoxModified(ReceiverModifiedType.ItemSelected, new ReceiverModifiedData(this));
        }
        #endregion
    }
}
