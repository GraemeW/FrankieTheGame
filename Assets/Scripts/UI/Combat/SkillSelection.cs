using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

        private void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
        }

        private void OnEnable()
        {
            Setup(battleController.GetActivePlayerCharacter());
            battleController.selectedPlayerCharacterChanged += Setup;
            battleController.battleInput += HandleInput;
            SetupEnemySlideButtons(true);
        }

        private void OnDisable()
        {
            battleController.selectedPlayerCharacterChanged -= Setup;
            battleController.battleInput -= HandleInput;
            SetupEnemySlideButtons(false);
        }

        private void SetupEnemySlideButtons(bool enable)
        {
            if (enemySlides == null) { return; }
            foreach (EnemySlide enemySlide in enemySlides)
            {
                enemySlide.GetComponent<Button>().onClick.RemoveAllListeners();
                if (enable)
                {
                    enemySlide.GetComponent<Button>().onClick.AddListener(delegate { ExecuteSkill(enemySlide.GetEnemy()); });
                }
            }
        }

        public void SetEnemySlides(List<EnemySlide> enemySlides)
        {
            this.enemySlides = enemySlides;
            if (gameObject.activeSelf) { SetupEnemySlideButtons(true); }
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

        public void HandleInput(string input) // PUBLIC:  Called via unity events for button clicks (mouse + keyboard)
        {
            if (SetBranch(input)) { return; }
            // TODO:  Add skill for highlighting / selecting out enemy to attack using keyboard instead of mouse
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

        private void ExecuteSkill(CombatParticipant recipient)
        {
            if (battleController.GetActivePlayerCharacter() != null && battleController.GetActiveSkill() != null)
            {
                battleController.AddToBattleQueue(battleController.GetActivePlayerCharacter(), recipient, battleController.GetActiveSkill());
            }
        }
    }
}