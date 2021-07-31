using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Frankie.Control;

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

        // State
        List<EnemySlide> enemySlides = null;

        // Cached References
        BattleController battleController = null;
        CanvasGroup canvasGroup = null;

        private void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            Setup(CombatParticipantType.Character, battleController.GetSelectedCharacter());
            battleController.selectedCombatParticipantChanged += Setup;
            battleController.battleInput += HandleInput;
        }

        private void OnDisable()
        {
            battleController.selectedCombatParticipantChanged -= Setup;
            battleController.battleInput -= HandleInput;
        }

        private void Setup(CombatParticipantType combatParticipantType, CombatParticipant combatParticipant)
        {
            if (combatParticipantType != CombatParticipantType.Character) { return; }

            if (combatParticipant == null ||
                battleController.GetActiveBattleAction().battleActionType == BattleActionType.ActionItem) // Do not pop skill selection if using an item
            { 
                SetAllFields(defaultNoText);
                canvasGroup.alpha = 0;
                return; 
            }
            canvasGroup.alpha = 1;

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
            if (up != null) { upField.text = Skill.GetSkillNamePretty(up.name); } else { upField.text = defaultNoText; }
            if (left != null) { leftField.text = Skill.GetSkillNamePretty(left.name); } else { leftField.text = defaultNoText; }
            if (right != null) { rightField.text = Skill.GetSkillNamePretty(right.name); } else { rightField.text = defaultNoText; }
            if (down != null) { downField.text = Skill.GetSkillNamePretty(down.name); } else { downField.text = defaultNoText; }

            Skill activeSkill = skillHandler.GetActiveSkill();
            if (activeSkill != null)
            { 
                skillField.text = Skill.GetSkillNamePretty(activeSkill.name);
                battleController.SetActiveBattleAction(new BattleAction(activeSkill));
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
