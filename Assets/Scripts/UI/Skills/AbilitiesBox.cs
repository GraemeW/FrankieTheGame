using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils.UI;

namespace Frankie.Combat.UI
{
    public class AbilitiesBox : SkillSelectionUI
    {
        // Tunables
        [Header("Abilities Box Attributes")]
        [SerializeField] private TMP_Text statText;
        [SerializeField] private TMP_Text skillDetailText;
        [SerializeField] private TMP_Text apCostText;
        [Header("Prefabs")]
        [SerializeField] private DialogueBox dialogueBoxPrefab;
        [Header("Messages")]
        [Tooltip("Include {0} for user, {1} for skill, {2} for target")][SerializeField] private string messageUseSkillInWorld = "{0} used {1} on {2}.";
        [Tooltip("Include {0} for user, {1} for item, {2} for target")][SerializeField] private string messageNotEnoughAP = "Not enough AP.";

        // State -- UI
        private List<BattleEntity> partyBattleEntities;
        private AbilitiesBoxState abilitiesBoxState = AbilitiesBoxState.InCharacterSelection;
        private readonly List<UIChoiceButton> playerSelectChoiceOptions = new();

        // State -- Objects
        private PartyCombatConduit partyCombatConduit;
        private BattleActionData battleActionData;

        // Cached References
        private List<CharacterSlide> characterSlides;

        // Events
        public event Action<CombatParticipantType, IEnumerable<BattleEntity>> targetCharacterChanged;

        #region PublicMethods
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit setPartyCombatConduit, List<CharacterSlide> setCharacterSlides)
        {
            controller = standardPlayerInputCaller;
            partyCombatConduit = setPartyCombatConduit;

            int choiceIndex = 0;
            partyBattleEntities = new List<BattleEntity>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
                UIChoiceButton uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.SetText(combatParticipant.GetCombatName());
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(combatParticipant); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(combatParticipant); });

                playerSelectChoiceOptions.Add(uiChoiceOption);
                partyBattleEntities.Add(new BattleEntity(combatParticipant));
                choiceIndex++;
            }
            RefreshUI(CombatParticipantType.Friendly, partyBattleEntities);

            characterSlides = setCharacterSlides;
            SubscribeCharacterSlides(true);

            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection);
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }

        private void SubscribeCharacterSlides(bool enable)
        {
            if (characterSlides == null) return;
            
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
                SetChoiceAvailable(true); // avoid short circuit on user control for other states
            }
        }

        protected override bool Choose(string nodeID)
        {
            switch (abilitiesBoxState)
            {
                case AbilitiesBoxState.InCharacterSelection:
                    return base.Choose(null);
                case AbilitiesBoxState.InAbilitiesSelection:
                    SkillHandler skillHandler = currentCombatParticipant?.GetComponent<SkillHandler>();
                    Skill activeSkill = skillHandler?.GetActiveSkill();
                    if (activeSkill == null) { return false; }

                    SetAbilitiesBoxState(AbilitiesBoxState.InCharacterTargeting);
                    if (!GetNextTarget(true))
                    {
                        SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
                        return false;
                    }
                    return true;
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
                RefreshUI(CombatParticipantType.Friendly, partyBattleEntities); // Failsafe, re-setup box if character lost
                SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection);
                return;
            }

            if (combatParticipant != currentCombatParticipant)
            {
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);

                currentCombatParticipant = combatParticipant;
                UpdateSkillHandler();
            }
            battleActionData = new BattleActionData(combatParticipant);
            SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);

            if (IsChoiceAvailable() && initializeCursor)
            {
                MoveCursor(PlayerInputType.NavigateRight);
            }
        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false);
            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection);
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
                dialogueBox.AddText(string.Format(messageUseSkillInWorld, senderName, skillName, targetCharacterNames));
                PassControl(dialogueBox);
                return true;
            }
            else
            {
                dialogueBox.AddText(string.Format(messageNotEnoughAP, senderName, skillName, targetCharacterNames));
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
                    switch (playerInputType)
                    {
                        case PlayerInputType.NavigateRight:
                        case PlayerInputType.NavigateDown:
                        {
                            if (!GetNextTarget(true))
                            {
                                SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
                                return false;
                            }
                            break;
                        }
                        case PlayerInputType.NavigateLeft:
                        case PlayerInputType.NavigateUp:
                        {
                            if (!GetNextTarget(false))
                            {
                                SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection);
                                return false;
                            }
                            break;
                        }
                    }
                    return true;
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
            HandleInputWithReturn(input);

            // Note:  Function re-use since standard implementation for SkillSelectionUI
            // Used explicitly w/ select skill && extended with Unity Events for mouse clicks
        }

        private bool GetNextTarget(bool? traverseForward)
        {
            if (currentCombatParticipant == null) { return false; }

            var skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            Skill activeSkill = skillHandler?.GetActiveSkill();
            if (activeSkill == null) { return false; }

            activeSkill.SetTargets(traverseForward, battleActionData, partyBattleEntities, null);
            if (battleActionData.targetCount == 0)
            {
                return false;
            }

            targetCharacterChanged?.Invoke(CombatParticipantType.Foe, battleActionData.GetTargets());
            return true;
        }

        private void UseSkillOnTarget(BattleEntity battleEntity)
        {
            // Don't care about state for mouse clicks, fail gracefully otherwise
            if (battleEntity == null) { return; }

            // Sanity against current character selection
            if (currentCombatParticipant == null) { ChooseCharacter(null); }
            if (currentCombatParticipant == null) { return; } // Something went wrong

            // Force a new battle action data
            battleActionData = new BattleActionData(currentCombatParticipant);

            battleActionData.SetTargets(battleEntity);
            if (!GetNextTarget(null)) { SetAbilitiesBoxState(AbilitiesBoxState.InAbilitiesSelection); return; } // Verify valid target by calling with null
            SetAbilitiesBoxState(AbilitiesBoxState.InCharacterTargeting);

            Choose(null);
        }

        protected override void PassSkillFlavour(SkillStat skillStat, string detail, float apCost)
        {
            statText.text = Enum.GetName(typeof(SkillStat), skillStat);
            if (detail != null)
            {
                skillDetailText.text = detail;
            }
            apCostText.text = $"{apCost:N0}";
        }
        #endregion

        #region AbilitiesBehaviour
        private void SetAbilitiesBoxState(AbilitiesBoxState setAbilitiesBoxState)
        {
            abilitiesBoxState = setAbilitiesBoxState;
            switch (abilitiesBoxState)
            {
                case AbilitiesBoxState.InCharacterSelection:
                    battleActionData = null; // Reset battle action data on selected character changed
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, null);
                    break;
                case AbilitiesBoxState.InAbilitiesSelection:
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, null);
                    break;
                case AbilitiesBoxState.InCharacterTargeting:
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, battleActionData.GetTargets()); // Re-highlight the target character
                    break;
            }
            SetUpChoiceOptions();

            OnUIBoxModified(UIBoxModifiedType.itemSelected, true);
        }

        protected override void ResetUI()
        {
            SetAllFields(defaultNoText);
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
                    skillField.text = defaultNoText;
                    statText.text = "";
                    skillDetailText.text = "";
                    apCostText.text = "";
                    SetAbilitiesBoxState(AbilitiesBoxState.InCharacterSelection);
                    return true;
                default:
                    return base.HandleGlobalInput(playerInputType);
            }
        }
        #endregion
    }
}
