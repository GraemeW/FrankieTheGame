using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public class SkillSelectionUI : UIBox<AbilitiesBoxState>, ILocalizable
    {
        // Tunables
        [Header("Skill Selection Text")]
        [SerializeField] protected string defaultNoText = "--";

        // State
        private bool usingBattleController = false;
        protected CombatParticipant selectedCharacter { get; private set; }
        protected SkillHandler selectedSkillHandler { get; private set; }
        
        private SkillSelectionModel skillSelectionModel;
        private SkillHandlerModel selectedSkillHandlerModel;
        private readonly Dictionary<SkillHandler, SkillHandlerModel> skillHandlerModels = new();

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

            var menuView = GetComponent<UIToolkitMenuView>();
            menuView.SetDataSource(skillSelectionModel);
            menuView.AddEntry(new SkillWheelHandle(menuView, skillSelectionModel, HandleInput));
        }

        protected override void StartTriggered()
        {
            RefreshSkillDisplay();
        }

        protected override void EnableTriggered()
        {
            if (usingBattleController)
            {
                SetVisible(false); // Shown once a character is selected
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

        protected override void DestroyTriggered()
        {
            ObserveSkillHandler(null);
            foreach (SkillHandlerModel skillHandlerModel in skillHandlerModels.Values) { skillHandlerModel.Dispose(); }
            skillHandlerModels.Clear();
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public virtual List<TableEntryReference> GetLocalizationEntries() => new();
        #endregion

        #region ModelMethods
        protected virtual SkillSelectionModel CreateSkillSelectionModel() => new();

        private void ObserveSkillHandler(SkillHandler skillHandler)
        {
            if (selectedSkillHandlerModel != null) { selectedSkillHandlerModel.propertyChanged -= HandleSkillHandlerChanged; }
            selectedSkillHandlerModel = skillHandler != null ? GetOrBuildSkillHandlerModel(skillHandler) : null;
            if (selectedSkillHandlerModel != null) { selectedSkillHandlerModel.propertyChanged += HandleSkillHandlerChanged; }
        }

        private SkillHandlerModel GetOrBuildSkillHandlerModel(SkillHandler skillHandler)
        {
            if (skillHandlerModels.TryGetValue(skillHandler, out SkillHandlerModel skillHandlerModel)) { return skillHandlerModel; }

            skillHandlerModel = new SkillHandlerModel(skillHandler);
            skillHandlerModels[skillHandler] = skillHandlerModel;
            return skillHandlerModel;
        }

        private void HandleSkillHandlerChanged(object sender, BindablePropertyChangedEventArgs propertyChangedEventArgs) => RefreshSkillDisplay();

        private void RefreshSkillDisplay()
        {
            if (selectedSkillHandlerModel != null)
            {
                skillSelectionModel.upSkillText = GetSkillText(selectedSkillHandlerModel.upSkill);
                skillSelectionModel.leftSkillText = GetSkillText(selectedSkillHandlerModel.leftSkill);
                skillSelectionModel.rightSkillText = GetSkillText(selectedSkillHandlerModel.rightSkill);
                skillSelectionModel.downSkillText = GetSkillText(selectedSkillHandlerModel.downSkill);
            }

            Skill activeSkill = selectedSkillHandlerModel?.activeSkill;
            skillSelectionModel.hasActiveSkill = activeSkill != null;
            skillSelectionModel.activeSkillText = GetSkillText(activeSkill);
        }

        private string GetSkillText(Skill skill) => skill != null ? skill.GetName() : defaultNoText;
        #endregion

        #region InputHandlers
        protected virtual void HandleInput(ControllerInputType input)
        {
            if (selectedSkillHandler == null) { return; }
            if (battleController.IsBattleActionArmed()) { return; } // Need to manually check because can be armed while UI element disabled (InventoryBox-based)
            SetBranchOrSkill(input);
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
            ResetUI(true);
        }

        protected void ResetUI(bool hide)
        {
            ObserveSkillHandler(null);
            skillSelectionModel.hasActiveSkill = false;
            skillSelectionModel.characterName = defaultNoText;
            skillSelectionModel.upSkillText = defaultNoText;
            skillSelectionModel.leftSkillText = defaultNoText;
            skillSelectionModel.rightSkillText = defaultNoText;
            skillSelectionModel.downSkillText = defaultNoText;
            skillSelectionModel.activeSkillText = defaultNoText;
            if (hide) { SetVisible(false); }
        }

        protected void RefreshUI(BattleEntitySelectionType selectionType, IEnumerable<BattleEntity> battleEntities)
        {
            if (selectionType != BattleEntitySelectionType.Actor) { return; }
            if (battleController != null)
            {
                // Do not pop skill selection if using an item
                if (battleController.GetActiveBattleAction() != null && battleController.GetActiveBattleAction().IsItem()) { return; }
            }

            SetSelectedCharacter(battleEntities?.FirstOrDefault()?.combatParticipant); // Expectation is single entry, handling edge case
            if (selectedSkillHandler == null) { ResetUI(); return; }

            UpdateSkillHandler();
        }

        // Note:  Skill handler is resolved once here - a character without one counts as no selection for skills
        protected void SetSelectedCharacter(CombatParticipant character)
        {
            selectedCharacter = character;
            selectedSkillHandler = character != null && character.TryGetComponent(out SkillHandler skillHandler) ? skillHandler : null;
        }

        protected void UpdateSkillHandler()
        {
            if (selectedSkillHandler == null) { return; }

            SetVisible(true);
            skillSelectionModel.characterName = selectedCharacter.GetCombatName();
            ObserveSkillHandler(selectedSkillHandler);
            selectedSkillHandler.ResetCurrentBranch();
            RefreshSkillDisplay();
        }

        protected bool SetBranchOrSkill(ControllerInputType input)
        {
            if (selectedSkillHandler == null) { return false; }

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

            // Note:  The display follows the skill handler via its model
            if (selectedSkillHandlerModel?.skillHandler != selectedSkillHandler) { ObserveSkillHandler(selectedSkillHandler); }
            selectedSkillHandler.SetBranchOrSkill(skillBranchMapping, SkillFilterType.All);

            Skill activeSkill = selectedSkillHandler.GetActiveSkill();
            if (activeSkill == null) { return true; }

            if (battleController != null) { battleController.SetActiveBattleAction(activeSkill); }
            TriggerUIBoxModified(ReceiverModifiedType.ItemSelected, new ReceiverModifiedData(this));
            PassSkillFlavour(activeSkill.GetStat(), activeSkill.GetDetail(), activeSkill.GetAPCost());
            return true;
        }

        protected void ResetSkillHandler()
        {
            if (selectedSkillHandler != null) { selectedSkillHandler.ResetCurrentBranch(); }
        }
        #endregion
    }
}
