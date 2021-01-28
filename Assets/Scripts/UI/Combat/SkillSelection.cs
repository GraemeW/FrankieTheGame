using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Frankie.Combat.UI
{
    public class SkillSelection : MonoBehaviour
    {
        // Tunables
        [SerializeField] TextMeshProUGUI selectedCharacterNameField = null;
        [SerializeField] TextMeshProUGUI upField = null;
        [SerializeField] TextMeshProUGUI leftField = null;
        [SerializeField] TextMeshProUGUI rightField = null;
        [SerializeField] TextMeshProUGUI downField = null;
        [SerializeField] TextMeshProUGUI skillField = null;
        [SerializeField] string defaultNoText = "--"; 

        // Cached References
        BattleController battleController = null;

        private void Awake()
        {
            battleController = FindObjectOfType<BattleController>();
        }

        private void OnEnable()
        {
            Setup(battleController.GetActivePlayerCharacter());
            battleController.selectedPlayerCharacterChanged += Setup;
            battleController.battleInput += HandleInput;
        }

        private void OnDisable()
        {
            battleController.selectedPlayerCharacterChanged -= Setup;
            battleController.battleInput -= HandleInput;
        }

        private void Setup(CombatParticipant combatParticipant)
        {
            if (combatParticipant == null) { SetAllFields(defaultNoText); return; }

            selectedCharacterNameField.text = combatParticipant.GetCombatName();
            SkillHandler skillHandler = combatParticipant.GetComponent<SkillHandler>();
            skillHandler.ResetCurrentBranch();
            UpdateSkills(skillHandler);
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
            if (up != null) { upField.text = up.name; } else { upField.text = defaultNoText; }
            if (left != null) { leftField.text = left.name; } else { leftField.text = defaultNoText; }
            if (right != null) { rightField.text = right.name; } else { rightField.text = defaultNoText; }
            if (down != null) { downField.text = down.name; } else { downField.text = defaultNoText; }

            Skill activeSkill = skillHandler.GetActiveSkill();
            if (activeSkill != null)
            { 
                skillField.text = activeSkill.name;
                battleController.SetActiveSkill(activeSkill);
            } 
            else { skillField.text = defaultNoText; }
        }

        public void HandleInput(string input)
        {
            if (SetBranch(input)) { return; }
            if (ExecuteSkill()) { return; }
        }

        private bool SetBranch(string input)
        {
            bool validInput = false;
            SkillBranchMapping skillBranchMapping = default;
            if (input == "up") { skillBranchMapping = SkillBranchMapping.up; validInput = true; }
            else if (input == "left") { skillBranchMapping = SkillBranchMapping.left; validInput = true; }
            else if (input == "right") { skillBranchMapping = SkillBranchMapping.right; validInput = true; }
            else if (input == "down") { skillBranchMapping = SkillBranchMapping.down; validInput = true; }

            if (validInput)
            {
                SkillHandler skillHandler = battleController.GetActivePlayerCharacter().GetComponent<SkillHandler>();
                skillHandler.SetBranch(skillBranchMapping);
                UpdateSkills(skillHandler);
            }
            return validInput;
        }

        private bool ExecuteSkill()
        {
            // TODO:  add some logic for selecting out via keyboard mapping here
            // TODO:  add to the battle queue
            return true;
        }
    }
}
