using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils.UI;
using System;
using System.Collections;
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
        AbilitiesBoxState abilitiesBoxState = AbilitiesBoxState.inCharacterSelection;
        List<UIChoiceOption> playerSelectChoiceOptions = new List<UIChoiceOption>();

        // State -- Objects
        Party party = null;
        IEnumerable<CombatParticipant> targetCharacters = null;

        // Cached References
        List<CharacterSlide> characterSlides = null;

        // Events
        public event Action<CombatParticipantType, IEnumerable<CombatParticipant>> targetCharacterChanged;

        #region UnityMethods
        // Revert to standard UIBox implementations
        protected override void Awake() { }
        protected override void OnEnable() { SubscribeCharacterSlides(true); StandardOnEnable();  }
        protected override void OnDisable() { SubscribeCharacterSlides(false); StandardOnDisable(); }
        #endregion

        #region PublicMethods
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, List<CharacterSlide> characterSlides)
        {
            controller = standardPlayerInputCaller;

            int choiceIndex = 0;
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject uiChoiceOptionObject = Instantiate(optionPrefab, optionParent);
                UIChoiceOption uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceOption>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.SetText(character.GetCombatName());
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(character); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(character); });

                playerSelectChoiceOptions.Add(uiChoiceOption);
                choiceIndex++;
            }
            this.party = party;
            Setup(CombatParticipantType.Character, party.GetParty());

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
                        characterSlide.AddButtonClickEvent(delegate { UseSkillOnTarget(characterSlide.GetCombatParticipant()); });
                    }
                    else
                    {
                        targetCharacterChanged -= characterSlide.HighlightSlide;
                        // Note:  Remove button click event listeners handled on battleSlide on disable (removes all listeners)
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

        protected void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            if (character == null)
            {
                currentCombatParticipant = null;
                SetAbilitiesBoxState(AbilitiesBoxState.inCharacterSelection);
                return;
            }

            if (character != currentCombatParticipant)
            {
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);

                currentCombatParticipant = character;
                RefreshSkills();
            }
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
            string targetCharacterNames = string.Join(", ", targetCharacters.Select(x => x.GetCombatName()).ToList());

            bool skillUsedSuccessfully = activeSkill.Use(currentCombatParticipant, targetCharacters, null);
            SetAbilitiesBoxState(AbilitiesBoxState.inAbilitiesSelection);

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
            // Used explicitlyselect skill && extended with Unity Events for mouse clicks
        }

        private bool GetNextTarget(bool? traverseForward)
        {
            if (abilitiesBoxState != AbilitiesBoxState.inCharacterTargeting) { return false; }
            if (currentCombatParticipant == null) { return false; }

            SkillHandler skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            Skill activeSkill = skillHandler?.GetActiveSkill();
            if (activeSkill == null) { return false; }

            targetCharacters = activeSkill.GetTargets(traverseForward, targetCharacters, party.GetParty(), null);
            if (targetCharacters == null || targetCharacters.Count() == 0)
            {
                return false;
            }

            targetCharacterChanged?.Invoke(CombatParticipantType.Target, targetCharacters);
            return true;
        }

        public void UseSkillOnTarget(CombatParticipant combatParticipant)
        {
            if (abilitiesBoxState != AbilitiesBoxState.inCharacterTargeting) { return; }

            targetCharacters = new[] { combatParticipant };
            if (!GetNextTarget(null)) { SetAbilitiesBoxState(AbilitiesBoxState.inAbilitiesSelection); return; }

            targetCharacterChanged?.Invoke(CombatParticipantType.Target, new[] { combatParticipant });
            Choose(null);
        }

        protected override void PassSkillFlavour(SkillStat skillStat, string detail, float apCost)
        {
            if (skillStat != SkillStat.None)
            {
                statText.text = Enum.GetName(typeof(SkillStat), skillStat);
            }
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
            SetUpChoiceOptions();

            if (abilitiesBoxState != AbilitiesBoxState.inCharacterTargeting)
            {
                targetCharacters = null;
                targetCharacterChanged?.Invoke(CombatParticipantType.Target, targetCharacters);
            }

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