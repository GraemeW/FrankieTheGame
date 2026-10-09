using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Stats.UI;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class AbilitiesBox : SkillSelectionUI
    {
        // Tunables
        [Header("Abilities Box Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedStatLabel;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedAPCostLabel;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageNotEnoughAP;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageNoValidTarget;
        [Header("Include {0} for user, {1} for skill, {2} for target")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageUseSkillInWorld;
        [Header("Prefabs")]
        [SerializeField] private DialogueBox dialogueBoxPrefab;

        // State -- UI
        private PartySelector partySelector;
        private AbilitiesBoxModel abilitiesBoxModel;

        // State
        private BattleActionData battleActionData;
        private DialogueBox abilityUseConfirmationBox;

        // Cached References
        private List<CharacterSlideHandle> characterSlides;

        // Events
        public event Action<BattleEntitySelectionType, IEnumerable<BattleEntity>> targetCharacterChanged;
        
        // UIBox Configuration
        protected override EnumLookup<AbilitiesBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var abilitiesConfiguration = new EnumLookup<AbilitiesBoxState, UIBoxStateBehaviour>();
            abilitiesConfiguration.TrySet(AbilitiesBoxState.InCharacterSelection, 
                new UIBoxStateBehaviour(
                    setupChoiceOptions: ImplementSetUpChoiceOptions,
                    reconcileChoiceOptions: ImplementReconcileChoiceOptions,
                    choose: _ => StandardChoose(null),
                    moveCursor: (input, _) => StandardMoveCursor(input, CursorMovementStyle.Horizontal))
            );
            abilitiesConfiguration.TrySet(AbilitiesBoxState.InAbilitiesSelection,
                new UIBoxStateBehaviour(
                    setupChoiceOptions: ImplementSetUpChoiceOptions,
                    reconcileChoiceOptions: ImplementReconcileChoiceOptions,
                    choose: _ => TryChooseSkill(),
                    moveCursor: (input, _) => HandleInputWithReturn(input),
                    tryHandleBackNavigation: TryBackFromAbilitiesSelection)
            );
            abilitiesConfiguration.TrySet(AbilitiesBoxState.InCharacterTargeting,
                new UIBoxStateBehaviour(
                    setupChoiceOptions: ImplementSetUpChoiceOptions,
                    reconcileChoiceOptions: ImplementReconcileChoiceOptions,
                    choose: _ => TryUseSkill(),
                    moveCursor: (input, _) => TryMoveCharacterTargeting(input),
                    tryHandleBackNavigation: TryBackFromCharacterTargeting)
            );
            return abilitiesConfiguration;
        }
        
        #region UnityMethods
        protected override void AwakeTriggered()
        {
            base.AwakeTriggered();
            uiState = AbilitiesBoxState.InCharacterSelection;
        }

        protected override void StartTriggered()
        {
            base.StartTriggered();
            abilitiesBoxModel.statLabel = localizedStatLabel.GetSafeLocalizedString();
            abilitiesBoxModel.apCostLabel = localizedAPCostLabel.GetSafeLocalizedString();
        }

        protected override void EnableTriggered()
        {
            base.EnableTriggered();
            SubscribeCharacterSlides(true);
        }

        protected override void DisableTriggered()
        {
            base.DisableTriggered();
            SubscribeCharacterSlides(false);
        }
        #endregion
        
        #region LocalizationMethods
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedStatLabel.TableEntryReference,
                localizedAPCostLabel.TableEntryReference,
                localizedMessageUseSkillInWorld.TableEntryReference,
                localizedMessageNotEnoughAP.TableEntryReference,
                localizedMessageNoValidTarget.TableEntryReference,
            };
        }
        #endregion
        
        #region Setup
        protected override SkillSelectionModel CreateSkillSelectionModel()
        {
            abilitiesBoxModel = new AbilitiesBoxModel();
            return abilitiesBoxModel;
        }

        public void Setup(BaseController baseController, PartyCombatConduit partyCombatConduit, List<CharacterSlideHandle> setCharacterSlides)
        {
            if (baseController == null || partyCombatConduit == null) { destroyQueued = true;  return; }
            
            controller = baseController;

            // Note:  Choice execution raises ItemSelected (sound) - avoid a second raise from the state change
            partySelector = new PartySelector(partyCombatConduit, AddNonDestroyChoiceOption, character => ChooseCharacter(character, true, false), SoftChooseCharacter);
            RefreshUI(BattleEntitySelectionType.Actor, partySelector.battleEntities);

            characterSlides = setCharacterSlides;
            SubscribeCharacterSlides(true);

            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection, true);
            ShowCursorOnAnyInteraction(ControllerInputType.Execute);
            if (partySelector.isSolo) { Choose(null); }
        }

        private void SubscribeCharacterSlides(bool enable)
        {
            if (characterSlides == null) { return; }
            
            foreach (CharacterSlideHandle characterSlide in characterSlides)
            {
                targetCharacterChanged -= characterSlide.HighlightSlide;
                characterSlide.RemoveButtonClickEvents();
                if (!enable) { characterSlide.HighlightSlide(BattleEntitySelectionType.Target, null); } // Clear any targeting highlight on exit
                if (enable)
                {
                    targetCharacterChanged += characterSlide.HighlightSlide;
                    characterSlide.AddButtonClickEvent(delegate { SlideUseSkillOnTarget(characterSlide.GetBattleEntity()); });
                }
            }
        }
        #endregion

        #region Interaction
        private void ImplementSetUpChoiceOptions()
        {
            choiceOptions.Clear();
            if (uiState == AbilitiesBoxState.InCharacterSelection && partySelector != null) { choiceOptions.AddRange(partySelector.choices); }
            ReconcileChoiceOptions();
        }

        private void ImplementReconcileChoiceOptions()
        {
            if (uiState == AbilitiesBoxState.InCharacterSelection)
            {
                SetChoiceAvailable(choiceOptions.Count > 0);
                return;
            }

            // Avoid short circuit on user control for other states
            SetChoiceAvailable(true);
        }

        private void ChooseCharacter(CombatParticipant combatParticipant, bool initializeCursor = true, bool triggerUIBoxModified = true)
        {
            if (combatParticipant == null)
            {
                // Failsafe, re-setup box if character lost
                SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection, false, triggerUIBoxModified);
                return;
            }
            
            SetSelectedCharacter(combatParticipant);
            battleActionData = new BattleActionData(combatParticipant);
            
            SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection, false, triggerUIBoxModified);
            if (IsChoiceAvailable() && initializeCursor) { MoveCursor(ControllerInputType.DefaultNone, CursorMovementStyle.Combined); }
        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false, false);
            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection, true, false);
        }

        private bool TryChooseSkill()
        {
            Skill activeSkill = selectedSkillHandler != null ? selectedSkillHandler.GetActiveSkill() : null;
            if (activeSkill == null) { return false; }

            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterTargeting);
            if (GetNextTarget(TargetingNavigationType.Hold)) { return true; }

            SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(localizedMessageNoValidTarget.GetSafeLocalizedString());
            controller.AddInputReceiver(dialogueBox, null);
            return false;
        }
        
        private bool TryMoveCharacterTargeting(ControllerInputType controllerInputType)
        {
            TargetingNavigationType targetingNavigationType = TargetingStrategy.ConvertPlayerInputToTargeting(controllerInputType);
            if (GetNextTarget(targetingNavigationType)) { return true; }
            
            SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
            return false;
        }

        private bool HandleInputWithReturn(ControllerInputType input) => SetBranchOrSkill(input);

        protected override void HandleInput(ControllerInputType input)
        {
            // Note:  Function re-use since standard implementation for SkillSelectionUI (keyboard + skill wheel clicks)
            HandleInputWithReturn(input);
        }

        private bool GetNextTarget(TargetingNavigationType targetingNavigationType, IEnumerable<BattleEntity> activeCharacters = null)
        {
            if (selectedSkillHandler == null) { return false; }

            Skill activeSkill = selectedSkillHandler.GetActiveSkill();
            if (activeSkill == null) { return false; }

            battleActionData ??= new BattleActionData(selectedCharacter);
            activeCharacters ??= partySelector.battleEntities;
            activeSkill.SetTargets(targetingNavigationType, battleActionData, activeCharacters, null);
            if (!battleActionData.HasTargets()) {return false; }

            targetCharacterChanged?.Invoke(BattleEntitySelectionType.Target, battleActionData.GetTargets());
            return true;
        }
        
        private bool TryUseSkill()
        {
            if (selectedSkillHandler == null) { return false; }

            Skill activeSkill = selectedSkillHandler.GetActiveSkill();
            if (activeSkill == null || battleActionData == null) { return false; }

            if (!battleActionData.HasTargets())
            {
                GetNextTarget(TargetingNavigationType.Hold);
                return false;
            }

            var senderName = selectedCharacter.GetCombatName();
            var skillName = activeSkill.GetName();
            var targetCharacterNames = string.Join(", ", battleActionData.GetTargets().Select(x => x.combatParticipant.GetCombatName()).ToList());
            bool skillUsedSuccessfully = activeSkill.Use(battleActionData, null); // Actual skill execution

            SpawnAbilityUseConfirmationBox(skillUsedSuccessfully, senderName, skillName, targetCharacterNames);
            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterTargeting); // After use, reset to character targeting -- for continuous skill use
            return skillUsedSuccessfully;
        }

        private void SlideUseSkillOnTarget(BattleEntity battleEntity)
        {
            battleActionData = new BattleActionData(selectedCharacter);
            if (!GetNextTarget(TargetingNavigationType.Hold, new[] { battleEntity })) { SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection); return; }
            TryUseSkill();
        }

        protected override void PassSkillFlavour(Stat skillStat, string detail, float apCost)
        {
            abilitiesBoxModel.statText = LocalizationNames.GetLocalizedName(skillStat);
            if (detail != null) { abilitiesBoxModel.detailText = detail; }
            abilitiesBoxModel.apCostText = $"{apCost:N0}";
        }

        private void SpawnAbilityUseConfirmationBox(bool skillUsedSuccessfully, string senderName, string skillName, string targetCharacterNames)
        {
            if (abilityUseConfirmationBox != null) { Destroy(abilityUseConfirmationBox.gameObject); }
            
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            abilityUseConfirmationBox = dialogueBox;

            abilityUseConfirmationBox.AddText(skillUsedSuccessfully ? 
                string.Format(localizedMessageUseSkillInWorld.GetSafeLocalizedString(), senderName, skillName, targetCharacterNames) : 
                string.Format(localizedMessageNotEnoughAP.GetSafeLocalizedString(), senderName, skillName, targetCharacterNames));
            controller.AddInputReceiver(dialogueBox, null);
        }
        #endregion

        #region AbilitiesBehaviour
        private void SetAbilitiesBoxState(AbilitiesBoxState setAbilitiesBoxState, bool bypassSoloCheck = false, bool triggerUIBoxModified = true)
        {
            uiState = setAbilitiesBoxState;
            switch (uiState)
            {
                case AbilitiesBoxState.InCharacterSelection:
                    if (!bypassSoloCheck && partySelector.isSolo)
                    {
                        destroyQueued = true;
                        return;
                    }
                    battleActionData = null; // Reset battle action data on selected character changed
                    ResetUI();
                    targetCharacterChanged?.Invoke(BattleEntitySelectionType.Target, null);
                    break;
                case AbilitiesBoxState.InAbilitiesSelection:
                    UpdateSkillHandler();
                    targetCharacterChanged?.Invoke(BattleEntitySelectionType.Target, null);
                    break;
                case AbilitiesBoxState.InCharacterTargeting:
                    targetCharacterChanged?.Invoke(BattleEntitySelectionType.Target, battleActionData.GetTargets()); // Re-highlight the target character
                    break;
            }
            SetUpChoiceOptions();
            if (triggerUIBoxModified) { TriggerUIBoxModified(ReceiverModifiedType.ItemSelected, new ReceiverModifiedData(this)); }
        }

        protected override void ResetUI()
        {
            ResetUI(false);
        }
        #endregion

        #region Interfaces
        private bool TryBackFromCharacterTargeting(ControllerInputType controllerInputType)
        {
            ResetSkillHandler();
            SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
            return true;
        }

        private bool TryBackFromAbilitiesSelection(ControllerInputType controllerInputType)
        {
            ResetSkillHandler();
            abilitiesBoxModel.statText = "";
            abilitiesBoxModel.detailText = "";
            abilitiesBoxModel.apCostText = "";
            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection);
            return true;
        }
        #endregion
    }
}
