using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Frankie.Control;
using Frankie.Utils.UI;

namespace Frankie.Combat.UI
{
    public class SkillSelectionUI : UIBox
    {
        // Tunables
        [SerializeField] TextMeshProUGUI selectedCharacterNameField = null;
        [SerializeField] TextMeshProUGUI upField = null;
        [SerializeField] TextMeshProUGUI leftField = null;
        [SerializeField] TextMeshProUGUI rightField = null;
        [SerializeField] TextMeshProUGUI downField = null;
        [SerializeField] protected TextMeshProUGUI skillField = null;
        [SerializeField] protected string defaultNoText = "--";

        // State
        protected CombatParticipant currentCombatParticipant = null;

        // Cached References
        BattleController battleController = null;

        #region VirtualMethods
        protected virtual void Awake()
        {
            controller = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
            battleController = controller as BattleController;
        }

        protected override void OnEnable()
        {
            BattleEventBus<BattleEntitySelectedEvent>.SubscribeToEvent(Setup);
            battleController.battleInput += HandleInput;
        }

        protected override void OnDisable()
        {
            BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(Setup);
            battleController.battleInput -= HandleInput;
        }

        protected virtual SkillHandler GetSelectedCharacter()
        {
            return battleController.GetSelectedCharacter().GetComponent<SkillHandler>();
        }

        protected virtual void HandleInput(PlayerInputType input)
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

        protected virtual void PassSkillFlavour(SkillStat skillStat, string detail, float apCost)
        {
            // Null implementation, for parsing in alternate context
        }

        protected virtual void ResetUI()
        {
            SetAllFields(defaultNoText);
            canvasGroup.alpha = 0;
        }
        #endregion

        #region PrivateMethods
        protected void Setup(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> battleEntities)
        {
            if (combatParticipantType != CombatParticipantType.Friendly) { return; }
            if (battleController != null)
            {
                // Do not pop skill selection if using an item
                if (battleController.GetActiveBattleAction() != null && battleController.GetActiveBattleAction().IsItem()) { return; } 
            }

            if (battleEntities == null) { ResetUI(); return; }

            CombatParticipant combatParticipant = battleEntities.First().combatParticipant; // Expectation is single entry, handling edge case
            currentCombatParticipant = combatParticipant;

            if (currentCombatParticipant == null) { ResetUI(); return; }

            RefreshSkills();
        }

        private void Setup(BattleEntitySelectedEvent battleEntitySelectedEvent)
        {
            Setup(battleEntitySelectedEvent.combatParticipantType, battleEntitySelectedEvent.battleEntities);
        }

        protected void RefreshSkills()
        {
            if (currentCombatParticipant == null) { return; }

            canvasGroup.alpha = 1;
            selectedCharacterNameField.text = currentCombatParticipant.GetCombatName();
            SkillHandler skillHandler = currentCombatParticipant.GetComponent<SkillHandler>();
            skillHandler.ResetCurrentBranch();
            UpdateSkills(skillHandler);
        }

        protected void SetAllFields(string text)
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
            skillHandler.GetPlayerSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down);
            if (up != null) { upField.text = Skill.GetSkillNamePretty(up.name); } else { upField.text = defaultNoText; }
            if (left != null) { leftField.text = Skill.GetSkillNamePretty(left.name); } else { leftField.text = defaultNoText; }
            if (right != null) { rightField.text = Skill.GetSkillNamePretty(right.name); } else { rightField.text = defaultNoText; }
            if (down != null) { downField.text = Skill.GetSkillNamePretty(down.name); } else { downField.text = defaultNoText; }

            Skill activeSkill = skillHandler.GetActiveSkill();
            if (activeSkill != null)
            { 
                skillField.text = Skill.GetSkillNamePretty(activeSkill.name);
                if (battleController != null) { battleController.SetActiveBattleAction(activeSkill); }
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);
            } 
            else { skillField.text = defaultNoText; }
        }

        protected bool SetBranchOrSkill(PlayerInputType input)
        {
            currentCombatParticipant = battleController.GetSelectedCharacter();
            return SetBranchOrSkill(currentCombatParticipant, input);
        }

        protected bool SetBranchOrSkill(CombatParticipant combatParticipant, PlayerInputType input)
        {
            if (combatParticipant == null) { return false; }

            bool validInput = false;
            SkillBranchMapping skillBranchMapping = default;
            if (input == PlayerInputType.NavigateUp) { skillBranchMapping = SkillBranchMapping.up; validInput = true; }
            else if (input == PlayerInputType.NavigateLeft) { skillBranchMapping = SkillBranchMapping.left; validInput = true; }
            else if (input == PlayerInputType.NavigateRight) { skillBranchMapping = SkillBranchMapping.right; validInput = true; }
            else if (input == PlayerInputType.NavigateDown) { skillBranchMapping = SkillBranchMapping.down; validInput = true; }

            if (validInput)
            {
                SkillHandler skillHandler = combatParticipant.GetComponent<SkillHandler>();
                skillHandler.SetBranchOrSkill(skillBranchMapping, SkillFilterType.All);
                UpdateSkills(skillHandler);
                Skill activeSkill = skillHandler.GetActiveSkill();
                if (activeSkill != null)
                {
                    PassSkillFlavour(activeSkill.GetStat(), activeSkill.GetDetail(), activeSkill.GetAPCost());
                }
            }
            return validInput;
        }

        protected void ResetSkillHandler(CombatParticipant combatParticipant)
        {
            SkillHandler skillHandler = combatParticipant.GetComponent<SkillHandler>();
            skillHandler.ResetCurrentBranch();
        }
        #endregion
    }
}
