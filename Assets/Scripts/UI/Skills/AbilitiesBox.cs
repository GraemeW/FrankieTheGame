using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using LowDefMustard.Control;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    public class AbilitiesBox : SkillSelectionUI
    {
        // Tunables
        [Header("Abilities Box Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedStatLabel;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedAPCostLabel;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageNotEnoughAP;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageNoValidTarget;
        [Header("Include {0} for user, {1} for skill, {2} for target")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageUseSkillInWorld;
        [Header("Abilities Box Hookups")]
        [SerializeField] private TMP_Text statLabelField;
        [SerializeField] private TMP_Text statTextField;
        [SerializeField] private TMP_Text apCostLabelField;
        [SerializeField] private TMP_Text apCostTextField;
        [SerializeField] private TMP_Text skillDetailTextField;
        [Header("Prefabs")]
        [SerializeField] private DialogueBox dialogueBoxPrefab;

        // State -- UI
        private List<BattleEntity> partyBattleEntities;
        private readonly List<UIChoiceButton> playerSelectChoiceOptions = new();

        // State
        private bool isPartySolo = false;
        private BattleActionData battleActionData;
        private DialogueBox abilityUseConfirmationBox;

        // Cached References
        private List<CharacterSlide> characterSlides;

        // Events
        public event Action<CombatParticipantType, IEnumerable<BattleEntity>> targetCharacterChanged;
        
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
            if (statLabelField != null) { statLabelField.SetText(localizedStatLabel.GetSafeLocalizedString());}
            if (apCostLabelField != null) { apCostLabelField.SetText(localizedAPCostLabel.GetSafeLocalizedString()); }
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
        public void Setup(BaseController baseController, PartyCombatConduit partyCombatConduit, List<CharacterSlide> setCharacterSlides)
        {
            if (baseController == null || partyCombatConduit == null) { destroyQueued = true;  return; }
            
            controller = baseController;
            isPartySolo = partyCombatConduit.IsPartySolo();

            SetupPartySelection(partyCombatConduit);
            RefreshUI(CombatParticipantType.Friendly, partyBattleEntities);

            characterSlides = setCharacterSlides;
            SubscribeCharacterSlides(true);

            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection, true);
            ShowCursorOnAnyInteraction(ControllerInputType.Execute);
            if (isPartySolo) { Choose(null); }
        }

        private void SetupPartySelection(PartyCombatConduit partyCombatConduit)
        {
            int choiceIndex = 0;
            partyBattleEntities = new List<BattleEntity>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
                var uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.DisableOnClickListeners();
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(combatParticipant); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(combatParticipant); });
                uiChoiceOption.SetText(combatParticipant.GetCombatName());
                uiChoiceOption.SetValidColor(choiceIndex == 0);
                uiChoiceOption.UseInvalidChoiceDimming(true);

                playerSelectChoiceOptions.Add(uiChoiceOption);
                partyBattleEntities.Add(new BattleEntity(combatParticipant));
                choiceIndex++;
            }
        }

        private void SubscribeCharacterSlides(bool enable)
        {
            if (characterSlides == null) { return; }
            
            foreach (CharacterSlide characterSlide in characterSlides)
            {
                targetCharacterChanged -= characterSlide.HighlightSlide;
                characterSlide.RemoveButtonClickEvents();
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
            if (uiState == AbilitiesBoxState.InCharacterSelection) { choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList()); }
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
            
            selectedCharacter = combatParticipant;
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
            SkillHandler skillHandler = selectedCharacter != null ? selectedCharacter.GetComponent<SkillHandler>() : null;
            Skill activeSkill = skillHandler != null ? skillHandler.GetActiveSkill() : null;
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

        private bool HandleInputWithReturn(ControllerInputType input)
        {
            return selectedCharacter != null && SetBranchOrSkill(selectedCharacter, input);
        }

        protected override void HandleInput(ControllerInputType input)
        {
            // Note:  Function re-use since standard implementation for SkillSelectionUI
            // Used explicitly w/ select skill && extended with Unity Events for mouse clicks
            HandleInputWithReturn(input);
        }

        private bool GetNextTarget(TargetingNavigationType targetingNavigationType, IList<BattleEntity> activeCharacters = null)
        {
            if (selectedCharacter == null) { return false; }

            var skillHandler = selectedCharacter.GetComponent<SkillHandler>();
            Skill activeSkill = skillHandler?.GetActiveSkill();
            if (activeSkill == null) { return false; }

            battleActionData ??= new BattleActionData(selectedCharacter);
            activeCharacters ??= partyBattleEntities;
            activeSkill.SetTargets(targetingNavigationType, battleActionData, activeCharacters, null);
            if (!battleActionData.HasTargets()) {return false; }

            targetCharacterChanged?.Invoke(CombatParticipantType.Foe, battleActionData.GetTargets());
            return true;
        }
        
        private bool TryUseSkill()
        {
            if (selectedCharacter == null) { return false; }

            var skillHandler = selectedCharacter.GetComponent<SkillHandler>();
            Skill activeSkill = skillHandler?.GetActiveSkill();
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
            statTextField.text = LocalizationNames.GetLocalizedName(skillStat);
            if (detail != null)
            {
                skillDetailTextField.text = detail;
            }
            apCostTextField.text = $"{apCost:N0}";
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
                    if (!bypassSoloCheck && isPartySolo)
                    {
                        destroyQueued = true;
                        return;
                    }
                    battleActionData = null; // Reset battle action data on selected character changed
                    ResetUI();
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, null);
                    break;
                case AbilitiesBoxState.InAbilitiesSelection:
                    UpdateSkillHandler();
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, null);
                    break;
                case AbilitiesBoxState.InCharacterTargeting:
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, battleActionData.GetTargets()); // Re-highlight the target character
                    break;
            }
            SetUpChoiceOptions();
            if (triggerUIBoxModified) { TriggerUIBoxModified(ReceiverModifiedType.ItemSelected, new ReceiverModifiedData(this)); }
        }

        protected override void ResetUI()
        {
            ResetUI(true, false);
        }
        #endregion

        #region Interfaces
        private bool TryBackFromCharacterTargeting(ControllerInputType controllerInputType)
        {
            ResetSkillHandler(selectedCharacter);
            SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
            return true;
        }

        private bool TryBackFromAbilitiesSelection(ControllerInputType controllerInputType)
        {
            ResetSkillHandler(selectedCharacter);
            skillField.SetText(defaultNoText);
            statTextField.text = "";
            skillDetailTextField.text = "";
            apCostTextField.text = "";
            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection);
            return true;
        }
        #endregion
    }
}
