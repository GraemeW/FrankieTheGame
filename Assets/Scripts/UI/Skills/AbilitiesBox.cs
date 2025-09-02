using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class AbilitiesBox : SkillSelectionUI
    {
        // Tunables
        [Header("Abilities Box Attributes")]
        [SerializeField] TMP_Text statText = null;
        [SerializeField] TMP_Text skillDetailText = null;
        [SerializeField] TMP_Text apCostText = null;
        [Header("Prefabs")]
        [SerializeField] DialogueBox dialogueBoxPrefab = null;
        [Header("Messages")]
        [Tooltip("Include {0} for user, {1} for skill, {2} for target")] [SerializeField] string messageUseSkillInWorld = "{0} used {1} on {2}.";
        [Tooltip("Include {0} for user, {1} for item, {2} for target")] [SerializeField] string messageNotEnoughAP = "Not enough AP.";

        // State -- UI
        List<BattleEntity> partyBattleEntities = null;
        AbilitiesBoxState abilitiesBoxState = AbilitiesBoxState.inCharacterSelection;
        List<UIChoiceButton> playerSelectChoiceOptions = new List<UIChoiceButton>();

        // State -- Objects
        PartyCombatConduit partyCombatConduit = null;
        BattleActionData battleActionData = null;

        // Cached References
        List<CharacterSlide> characterSlides = null;

        // Events
        public event Action<CombatParticipantType, IEnumerable<BattleEntity>> targetCharacterChanged;

        #region UnityMethods
        // Revert to standard UIBox implementations
        protected override void Awake() { }
        protected override void OnEnable() { SubscribeCharacterSlides(true); StandardOnEnable();  }
        protected override void OnDisable() { SubscribeCharacterSlides(false); StandardOnDisable(); }
        #endregion

        #region PublicMethods
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, List<CharacterSlide> characterSlides)
        {
            controller = standardPlayerInputCaller;
            this.partyCombatConduit = partyCombatConduit;

            int choiceIndex = 0;
            partyBattleEntities = new List<BattleEntity>();
            foreach (CombatParticipant combatParticipant in this.partyCombatConduit.GetPartyCombatParticipants())
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
            Setup(CombatParticipantType.Friendly, partyBattleEntities);

            this.characterSlides = characterSlides;
            SubscribeCharacterSlides(true);

            SetAbilitiesBoxState(AbilitiesBoxState.inCharacterSelection);
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }

        private void SubscribeCharacterSlides(bool enable)
        {
            if (characterSlides != null)
            {
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
        }
        #endregion

        #region Interaction
        protected override void SetUpChoiceOptions()
        {
            choiceOptions.Clear();
            if (abilitiesBoxState == AbilitiesBoxState.inCharacterSelection)
            {
                choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
                SetChoiceAvailable(choiceOptions.Count > 0);
            }
            else
            {
                SetChoiceAvailable(true); // avoid short circuit on user control for other states
                return;
            }
        }

        protected override bool Choose(string nodeID)
        {
            switch (abilitiesBoxState)
            {
                case AbilitiesBoxState.inCharacterSelection:
                    return base.Choose(null);
                case AbilitiesBoxState.inAbilitiesSelection:
                    SkillHandler skillHandler = currentCombatParticipant?.GetComponent<SkillHandler>();
                    Skill activeSkill = skillHandler?.GetActiveSkill();
                    if (activeSkill == null) { return false; }

                    SetAbilitiesBoxState(AbilitiesBoxState.inCharacterTargeting);
                    if (!GetNextTarget(true))
                    {
                        SetAbilitiesBoxState(AbilitiesBoxState.inAbilitiesSelection);
                        return false;
                    }
                    return true;
                case AbilitiesBoxState.inCharacterTargeting:
                    return ChooseSkill();
                default:
                    return false;
            }
        }

        protected void ChooseCharacter(CombatParticipant combatParticipant, bool initializeCursor = true)
        {
            if (combatParticipant == null)
            {
                Setup(CombatParticipantType.Friendly, partyBattleEntities); // Failsafe, re-setup box if character lost
                SetAbilitiesBoxState(AbilitiesBoxState.inCharacterSelection);
                return;
            }

            if (combatParticipant != currentCombatParticipant)
            {
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);

                currentCombatParticipant = combatParticipant;
                RefreshSkills();
            }
            battleActionData = new BattleActionData(combatParticipant);
            SetAbilitiesBoxState(AbilitiesBoxState.inAbilitiesSelection);

            if (IsChoiceAvailable() && initializeCursor)
            {
                MoveCursor(PlayerInputType.NavigateRight);
            }
        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false);
            SetAbilitiesBoxState(AbilitiesBoxState.inCharacterSelection);
        }

        private bool ChooseSkill()
        {
            if (currentCombatParticipant == null) { return false; }

            SkillHandler skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            Skill activeSkill = skillHandler?.GetActiveSkill();
            if (activeSkill == null) { return false; }

            string senderName = currentCombatParticipant.GetCombatName();
            string skillName = activeSkill.GetName();
            string targetCharacterNames = string.Join(", ", battleActionData.GetTargets().Select(x => x.combatParticipant.GetCombatName()).ToList());

            bool skillUsedSuccessfully = activeSkill.Use(battleActionData, null); // Actual skill execution

            SetAbilitiesBoxState(AbilitiesBoxState.inCharacterTargeting); // After use, reset to character targeting -- for continuous skill use

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
                case AbilitiesBoxState.inCharacterSelection:
                    return base.MoveCursor(playerInputType);
                case AbilitiesBoxState.inAbilitiesSelection:
                    return HandleInputWithReturn(playerInputType);
                case AbilitiesBoxState.inCharacterTargeting:
                    if (playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
                    {
                        if (!GetNextTarget(true))
                        {
                            SetAbilitiesBoxState(AbilitiesBoxState.inAbilitiesSelection);
                            return false;
                        }
                    }
                    else if (playerInputType == PlayerInputType.NavigateLeft || playerInputType == PlayerInputType.NavigateUp)
                    {
                        if (!GetNextTarget(false))
                        {
                            SetAbilitiesBoxState(AbilitiesBoxState.inAbilitiesSelection);
                            return false;
                        }
                    }
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleInputWithReturn(PlayerInputType input)
        {
            if (currentCombatParticipant == null) { return false; }
            if (SetBranchOrSkill(currentCombatParticipant, input)) { return true; }

            return false;
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

            SkillHandler skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            Skill activeSkill = skillHandler?.GetActiveSkill();
            if (activeSkill == null) { return false; }

            activeSkill.GetTargets(traverseForward, battleActionData, partyBattleEntities, null);
            if (battleActionData.targetCount == 0)
            {
                return false;
            }

            targetCharacterChanged?.Invoke(CombatParticipantType.Foe, battleActionData.GetTargets());
            return true;
        }

        public void UseSkillOnTarget(BattleEntity battleEntity)
        {
            // Don't care about state for mouse clicks, fail gracefully otherwise
            if (battleEntity == null) { return; }

            // Sanity against current character selection
            if (currentCombatParticipant == null) { ChooseCharacter(null); }
            if (currentCombatParticipant == null) { return; } // Something went wrong

            // Force a new battle action data
            battleActionData = new BattleActionData(currentCombatParticipant);

            battleActionData.SetTargets(battleEntity);
            if (!GetNextTarget(null)) { SetAbilitiesBoxState(AbilitiesBoxState.inAbilitiesSelection); return; } // Verify valid target by calling with null
            SetAbilitiesBoxState(AbilitiesBoxState.inCharacterTargeting);

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
        private void SetAbilitiesBoxState(AbilitiesBoxState abilitiesBoxState)
        {
            this.abilitiesBoxState = abilitiesBoxState;
            switch (abilitiesBoxState)
            {
                case AbilitiesBoxState.inCharacterSelection:
                    battleActionData = null; // Reset battle action data on selected character changed
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, null);
                    break;
                case AbilitiesBoxState.inAbilitiesSelection:
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, null);
                    break;
                case AbilitiesBoxState.inCharacterTargeting:
                    targetCharacterChanged?.Invoke(CombatParticipantType.Foe, battleActionData.GetTargets()); // Re-highlight the target character
                    break;
                default:
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

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (abilitiesBoxState == AbilitiesBoxState.inCharacterTargeting)
                {
                    ResetSkillHandler(currentCombatParticipant);
                    SetAbilitiesBoxState(AbilitiesBoxState.inAbilitiesSelection);
                    return true;
                }
                else if (abilitiesBoxState == AbilitiesBoxState.inAbilitiesSelection)
                {
                    ResetSkillHandler(currentCombatParticipant);
                    skillField.text = defaultNoText;
                    statText.text = "";
                    skillDetailText.text = "";
                    apCostText.text = "";
                    SetAbilitiesBoxState(AbilitiesBoxState.inCharacterSelection);
                    return true;
                }
            }
            return base.HandleGlobalInput(playerInputType);
        }
        #endregion
    }
}