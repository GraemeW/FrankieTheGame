using Frankie.Combat;
using Frankie.Control;
using Frankie.Speech.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Frankie.Utils;

namespace Frankie.Stats.UI
{
    public class StatusBox : DialogueOptionBox
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] TextMeshProUGUI selectedCharacterNameField;
        [SerializeField] TextMeshProUGUI experienceToLevel;
        [Header("Parents")]
        [SerializeField] Transform leftStatParent = null;
        [SerializeField] Transform rightStatParent = null;
        [Header("Prefabs")]
        [SerializeField] GameObject statFieldPrefab = null;

        // State
        CombatParticipant selectedCharacter = null;

        public override void Setup(string optionText)
        {
            // Do Nothing (skip base implementation)
        }

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party)
        {
            SetGlobalInputHandler(standardPlayerInputCaller);
            int choiceIndex = 0;
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject characterFieldObject = Instantiate(optionPrefab, optionParent);
                DialogueChoiceOption dialogueChoiceOption = characterFieldObject.GetComponent<DialogueChoiceOption>();
                dialogueChoiceOption.SetChoiceOrder(choiceIndex);
                dialogueChoiceOption.SetText(character.GetCombatName());
                characterFieldObject.GetComponent<Button>().onClick.AddListener(delegate { ChooseCharacter(character); });
                dialogueChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(character); });

                if (choiceIndex == 0) { SoftChooseCharacter(character); }
                choiceIndex++;
            }
            SetUpChoiceOptions();
        }

        private void ChooseCharacter(CombatParticipant character)
        {
            // No actions currently available in character choice
            // TODO:  Implement pop-up to remove character from party
        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            if (character != selectedCharacter)
            {
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);

                selectedCharacter = character;
                CleanUpOldStats();

                selectedCharacterNameField.text = selectedCharacter.GetCombatName();
                experienceToLevel.text = selectedCharacter.GetComponent<Experience>().GetExperienceRequiredToLevel().ToString();

                GenerateLevel(character);
                GenerateHPAP(character);
                GenerateSkillStats(character);
            }
        }

        private void CleanUpOldStats()
        {
            foreach (Transform child in leftStatParent) { Destroy(child.gameObject); }
            foreach (Transform child in rightStatParent) { Destroy(child.gameObject); }
        }

        private void GenerateLevel(CombatParticipant character)
        {
            GameObject levelFieldObject = Instantiate(statFieldPrefab, leftStatParent);
            levelFieldObject.GetComponent<StatField>().Setup("Level", character.GetLevel());
        }

        private void GenerateHPAP(CombatParticipant character)
        {
            GameObject hpFieldObject = Instantiate(statFieldPrefab, rightStatParent);
            hpFieldObject.GetComponent<StatField>().Setup("HP", character.GetHP(), character.GetMaxHP());

            GameObject apFieldObject = Instantiate(statFieldPrefab, rightStatParent);
            apFieldObject.GetComponent<StatField>().Setup("AP", character.GetAP(), character.GetMaxAP());
        }

        private void GenerateSkillStats(CombatParticipant character)
        {
            Array skillStats = Enum.GetValues(typeof(SkillStat));
            foreach (SkillStat skillStat in skillStats)
            {
                if (skillStat == SkillStat.None) { continue; }
                Stat stat = (Stat)Enum.Parse(typeof(Stat), skillStat.ToString());

                GameObject statFieldObject = Instantiate(statFieldPrefab, leftStatParent);
                float statValue = character.GetBaseStats().GetStat(stat);
                statFieldObject.GetComponent<StatField>().Setup(stat, statValue);
            }
        }
    }
}