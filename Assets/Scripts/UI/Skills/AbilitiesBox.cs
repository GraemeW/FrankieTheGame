using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Control;
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
        private AbilitiesBoxState abilitiesBoxState = AbilitiesBoxState.InCharacterSelection;
        private readonly List<UIChoiceButton> playerSelectChoiceOptions = new();

        // State
        private bool isPartySolo = false;
        private BattleActionData battleActionData;

        // Cached References
        private List<CharacterSlide> characterSlides;

        // Events
        public event Action<CombatParticipantType, IEnumerable<BattleEntity>> targetCharacterChanged;

        #region UnityMethods

        protected override void Start()
        {
            base.Start();
            if (statLabelField != null) { statLabelField.SetText(localizedStatLabel.GetSafeLocalizedString());}
            if (apCostLabelField != null) { apCostLabelField.SetText(localizedAPCostLabel.GetSafeLocalizedString()); }
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
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, List<CharacterSlide> setCharacterSlides)
        {
            if (standardPlayerInputCaller == null || partyCombatConduit == null) { destroyQueued = true;  return; }
            
            controller = standardPlayerInputCaller;
            isPartySolo = partyCombatConduit.IsPartySolo();

            SetupPartySelection(partyCombatConduit);
            RefreshUI(CombatParticipantType.Friendly, partyBattleEntities);

            characterSlides = setCharacterSlides;
            SubscribeCharacterSlides(true);

            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection, true);
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
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
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(combatParticipant); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(combatParticipant); });
                uiChoiceOption.SetText(combatParticipant.GetCombatName());
                uiChoiceOption.SetValidColor(choiceIndex == 0);
                uiChoiceOption.UseHighlightColor(true);

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
                if (enable)
                {
                    targetCharacterChanged += characterSlide.HighlightSlide;
                    characterSlide.AddButtonClickEvent(delegate { UseSkillOnTarget(characterSlide.GetBattleEntity()); });
                }
                else
                {
                    targetCharacterChanged -= characterSlide.HighlightSlide;
                    characterSlide.RemoveButtonClickEvents();
                }
            }
        }
        #endregion

        #region Interaction
        protected override void SetUpChoiceOptions()
        {
            choiceOptions.Clear();
            if (abilitiesBoxState == AbilitiesBoxState.InCharacterSelection)
            {
                choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
                SetChoiceAvailable(choiceOptions.Count > 0);
            }
            else
            {
                // Avoid short circuit on user control for other states
                SetChoiceAvailable(true);
            }
        }

        protected override bool Choose(string nodeID)
        {
            switch (abilitiesBoxState)
            {
                case AbilitiesBoxState.InCharacterSelection:
                    return base.Choose(null);
                case AbilitiesBoxState.InAbilitiesSelection:
                    var skillHandler = currentCombatParticipant?.GetComponent<SkillHandler>();
                    Skill activeSkill = skillHandler?.GetActiveSkill();
                    if (activeSkill == null) { return false; }

                    SetAbilitiesBoxState(AbilitiesBoxState.InCharacterTargeting);
                    if (GetNextTarget(TargetingNavigationType.Hold)) { return true; }
                    else
                    {
                        SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
                        DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
                        dialogueBox.AddText(localizedMessageNoValidTarget.GetSafeLocalizedString());
                        PassControl(dialogueBox);
                        return false;
                    }
                case AbilitiesBoxState.InCharacterTargeting:
                    return ChooseSkill();
                default:
                    return false;
            }
        }

        private void ChooseCharacter(CombatParticipant combatParticipant, bool initializeCursor = true)
        {
            if (combatParticipant == null)
            {
                // Failsafe, re-setup box if character lost
                SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection);
                return;
            }

            if (combatParticipant != currentCombatParticipant)
            {
                OnUIBoxModified(UIBoxModifiedType.ItemSelected, true);
                currentCombatParticipant = combatParticipant;
            }
            
            battleActionData = new BattleActionData(combatParticipant);
            SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);

            if (IsChoiceAvailable() && initializeCursor)
            {
                MoveCursor(PlayerInputType.DefaultNone);
            }
        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false);
            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection, true);
        }

        private bool ChooseSkill()
        {
            if (currentCombatParticipant == null) { return false; }

            var skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            Skill activeSkill = skillHandler?.GetActiveSkill();
            if (activeSkill == null) { return false; }

            var senderName = currentCombatParticipant.GetCombatName();
            var skillName = activeSkill.GetName();
            var targetCharacterNames = string.Join(", ", battleActionData.GetTargets().Select(x => x.combatParticipant.GetCombatName()).ToList());

            bool skillUsedSuccessfully = activeSkill.Use(battleActionData, null); // Actual skill execution

            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterTargeting); // After use, reset to character targeting -- for continuous skill use

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            if (skillUsedSuccessfully)
            {
                dialogueBox.AddText(string.Format(localizedMessageUseSkillInWorld.GetSafeLocalizedString(), senderName, skillName, targetCharacterNames));
                PassControl(dialogueBox);
                return true;
            }
            else
            {
                dialogueBox.AddText(string.Format(localizedMessageNotEnoughAP.GetSafeLocalizedString(), senderName, skillName, targetCharacterNames));
                PassControl(dialogueBox);
                return false;
            }
        }

        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            switch (abilitiesBoxState)
            {
                case AbilitiesBoxState.InCharacterSelection:
                    return base.MoveCursor(playerInputType);
                case AbilitiesBoxState.InAbilitiesSelection:
                    return HandleInputWithReturn(playerInputType);
                case AbilitiesBoxState.InCharacterTargeting:
                {
                    TargetingNavigationType targetingNavigationType = TargetingStrategy.ConvertPlayerInputToTargeting(playerInputType);
                    if (GetNextTarget(targetingNavigationType)) { return true; }
                    
                    SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
                    return false;
                }
                default:
                    return false;
            }
        }

        private bool HandleInputWithReturn(PlayerInputType input)
        {
            return currentCombatParticipant != null && SetBranchOrSkill(currentCombatParticipant, input);
        }

        protected override void HandleInput(PlayerInputType input)
        {
            // Note:  Function re-use since standard implementation for SkillSelectionUI
            // Used explicitly w/ select skill && extended with Unity Events for mouse clicks
            HandleInputWithReturn(input);
        }

        private bool GetNextTarget(TargetingNavigationType targetingNavigationType)
        {
            if (currentCombatParticipant == null) { return false; }

            var skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            Skill activeSkill = skillHandler?.GetActiveSkill();
            if (activeSkill == null) { return false; }

            activeSkill.SetTargets(targetingNavigationType, battleActionData, partyBattleEntities, null);
            if (!battleActionData.HasTargets()) {return false; }

            targetCharacterChanged?.Invoke(CombatParticipantType.Foe, battleActionData.GetTargets());
            return true;
        }

        private void UseSkillOnTarget(BattleEntity battleEntity)
        {
            // Do not care about state for mouse clicks, fail gracefully otherwise
            if (battleEntity == null) { return; }

            // Sanity against current character selection
            if (currentCombatParticipant == null) { ChooseCharacter(null); }
            if (currentCombatParticipant == null) { return; } // Something went wrong

            // Force a new battle action data
            battleActionData = new BattleActionData(currentCombatParticipant);

            battleActionData.SetTargets(battleEntity);
            if (!GetNextTarget(TargetingNavigationType.Hold)) { SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection); return; } // Verify valid target by calling with null
            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterTargeting);

            Choose(null);
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
        #endregion

        #region AbilitiesBehaviour
        private void SetAbilitiesBoxState(AbilitiesBoxState setAbilitiesBoxState, bool bypassSoloCheck = false)
        {
            abilitiesBoxState = setAbilitiesBoxState;
            switch (abilitiesBoxState)
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

            OnUIBoxModified(UIBoxModifiedType.ItemSelected, true);
        }

        protected override void ResetUI()
        {
            ResetUI(true, false);
        }
        #endregion

        #region Interfaces
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType != PlayerInputType.Option && playerInputType != PlayerInputType.Cancel) { return base.HandleGlobalInput(playerInputType); }
            switch (abilitiesBoxState)
            {
                case AbilitiesBoxState.InCharacterTargeting:
                    ResetSkillHandler(currentCombatParticipant);
                    SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
                    return true;
                case AbilitiesBoxState.InAbilitiesSelection:
                    ResetSkillHandler(currentCombatParticipant);
                    skillField.SetText(defaultNoText);
                    statTextField.text = "";
                    skillDetailTextField.text = "";
                    apCostTextField.text = "";
                    SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection);
                    return true;
                default:
                    return base.HandleGlobalInput(playerInputType);
            }
        }
        #endregion
    }
}
