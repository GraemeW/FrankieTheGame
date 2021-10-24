using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Frankie.Control;
using Frankie.Speech.UI;
using System.Linq;

namespace Frankie.Combat.UI
{
    public class SkillSelectionUI : DialogueBox
    {
        // Tunables
        [SerializeField] TextMeshProUGUI selectedCharacterNameField = null;
        [SerializeField] TextMeshProUGUI upField = null;
        [SerializeField] TextMeshProUGUI leftField = null;
        [SerializeField] TextMeshProUGUI rightField = null;
        [SerializeField] TextMeshProUGUI downField = null;
        [SerializeField] TextMeshProUGUI skillField = null;
        [SerializeField] string defaultNoText = "--";

        // State
        CombatParticipant currentCombatParticipant = null;

        // Cached References
        BattleController battleController = null;

        protected override void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
        }

        protected override void OnEnable()
        {
            battleController.selectedCombatParticipantChanged += Setup;
            battleController.battleInput += HandleInput;
        }

        protected override void OnDisable()
        {
            battleController.selectedCombatParticipantChanged -= Setup;
            battleController.battleInput -= HandleInput;
        }

        private void Setup(CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> combatParticipants)
        {
            if (combatParticipantType != CombatParticipantType.Character) { return; }
            if (combatParticipants == null) { ResetUI(); return; }

            CombatParticipant combatParticipant = combatParticipants.First(); // Expectation is single entry, handling edge case
            currentCombatParticipant = combatParticipant;

            if (currentCombatParticipant == null || 
                (battleController.GetActiveBattleAction() != null && battleController.GetActiveBattleAction().IsItem())) // Do not pop skill selection if using an item
            {
                ResetUI();
                return; 
            }
            canvasGroup.alpha = 1;

            selectedCharacterNameField.text = currentCombatParticipant.GetCombatName();
            SkillHandler skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            skillHandler.ResetCurrentBranch();
            UpdateSkills(skillHandler);
        }

        private void ResetUI()
        {
            SetAllFields(defaultNoText);
            canvasGroup.alpha = 0;
        }

        private void SetAllFields(string text)
        {
            selectedCharacterNameField.text = text;
            upField.text = text;
            leftField.text = text;
            rightField.text = text;
            downField.text = text;
            skillField.text = text;
        }

        private void UpdateSkills(SkillHandler skillHandler)
        {
            skillHandler.GetSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down);
            if (up != null) { upField.text = Skill.GetSkillNamePretty(up.name); } else { upField.text = defaultNoText; }
            if (left != null) { leftField.text = Skill.GetSkillNamePretty(left.name); } else { leftField.text = defaultNoText; }
            if (right != null) { rightField.text = Skill.GetSkillNamePretty(right.name); } else { rightField.text = defaultNoText; }
            if (down != null) { downField.text = Skill.GetSkillNamePretty(down.name); } else { downField.text = defaultNoText; }

            Skill activeSkill = skillHandler.GetActiveSkill();
            if (activeSkill != null)
            { 
                skillField.text = Skill.GetSkillNamePretty(activeSkill.name);
                battleController.SetActiveBattleAction(activeSkill);
                OnDialogueBoxModified(DialogueBoxModifiedType.itemSelected, true);
            } 
            else { skillField.text = defaultNoText; }
        }

        public void HandleInput(PlayerInputType input) 
        {
            if (battleController.GetSelectedCharacter() == null) { return; }
            if (!battleController.IsBattleActionArmed())
            {
                if (SetBranchOrSkill(input)) { return; }
            }
        }

        public void HandleInput(int input) // PUBLIC:  Called via unity events for button clicks (mouse)
        {
            // Because Unity hates handling enums
            PlayerInputType battleInputType = (PlayerInputType)input;
            HandleInput(battleInputType);
        }

        private bool SetBranchOrSkill(PlayerInputType input)
        {
            bool validInput = false;
            SkillBranchMapping skillBranchMapping = default;
            if (input == PlayerInputType.NavigateUp) { skillBranchMapping = SkillBranchMapping.up; validInput = true; }
            else if (input == PlayerInputType.NavigateLeft) { skillBranchMapping = SkillBranchMapping.left; validInput = true; }
            else if (input == PlayerInputType.NavigateRight) { skillBranchMapping = SkillBranchMapping.right; validInput = true; }
            else if (input == PlayerInputType.NavigateDown) { skillBranchMapping = SkillBranchMapping.down; validInput = true; }

            if (validInput)
            {
                SkillHandler skillHandler = battleController.GetSelectedCharacter().GetComponent<SkillHandler>();
                skillHandler.SetBranchOrSkill(skillBranchMapping);
                UpdateSkills(skillHandler);
            }
            return validInput;
        }
    }
}
