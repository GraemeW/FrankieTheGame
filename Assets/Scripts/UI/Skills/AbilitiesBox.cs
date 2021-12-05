using Frankie.Control;
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

        // State
        AbilitiesBoxState abilitiesBoxState = AbilitiesBoxState.inCharacterSelection;
        List<UIChoiceOption> playerSelectChoiceOptions = new List<UIChoiceOption>();

        #region UnityMethods
        // Revert to standard UIBox implementations
        protected override void Awake() { }
        protected override void OnEnable() { StandardOnEnable(); }
        protected override void OnDisable() { StandardOnDisable(); }
        #endregion

        #region PublicMethods
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party)
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
            Setup(CombatParticipantType.Character, party.GetParty());
            SetAbilitiesBoxState(AbilitiesBoxState.inCharacterSelection);
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }
        #endregion

        #region Interaction
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (abilitiesBoxState == AbilitiesBoxState.inCharacterSelection)
            {
                return base.MoveCursor(playerInputType);
            }
            else if (abilitiesBoxState == AbilitiesBoxState.inAbilitiesSelection)
            {
                HandleInput(playerInputType);
                return true;
            }
            return false;
        }

        protected override bool Choose(string nodeID)
        {
            if (abilitiesBoxState == AbilitiesBoxState.inCharacterSelection)
            {
                return base.Choose(null);
            }
            return false;
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

        protected override void HandleInput(PlayerInputType input)
        {
            if (currentCombatParticipant == null) { return; }
            if (SetBranchOrSkill(currentCombatParticipant, input)) { return; }
        }

        protected override void PassSkillFlavour(SkillStat skillStat, string detail)
        {
            if (skillStat != SkillStat.None)
            {
                statText.text = Enum.GetName(typeof(SkillStat), skillStat);
            }
            if (detail != null)
            {
                skillDetailText.text = detail;
            }
        }
        #endregion

        #region AbilitiesBehaviour
        private void SetAbilitiesBoxState(AbilitiesBoxState abilitiesBoxState)
        {
            this.abilitiesBoxState = abilitiesBoxState;
            SetUpChoiceOptions();
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
                if (abilitiesBoxState == AbilitiesBoxState.inAbilitiesSelection)
                {
                    ResetSkillHandler(currentCombatParticipant);
                    statText.text = "";
                    skillDetailText.text = "";
                    SetAbilitiesBoxState(AbilitiesBoxState.inCharacterSelection);
                    return true;
                }
            }
            return base.HandleGlobalInput(playerInputType);
        }
        #endregion
    }
}