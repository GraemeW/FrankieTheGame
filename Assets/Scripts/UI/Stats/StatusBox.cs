using Frankie.Combat;
using Frankie.Utils.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

namespace Frankie.Stats.UI
{
    public class StatusBox : UIBox
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] TextMeshProUGUI selectedCharacterNameField;
        [SerializeField] TextMeshProUGUI experienceToLevel;
        [Header("Parents")]
        [SerializeField] Transform leftStatParent = null;
        [SerializeField] Transform rightStatParent = null;
        [Header("Prefabs")]
        [SerializeField] StatField statFieldPrefab = null;

        // State
        CombatParticipant selectedCharacter = null;

        public void Setup(PartyCombatConduit partyCombatConduit)
        {
            int choiceIndex = 0;
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants())
            {
                GameObject uiChoiceOptionObject = Instantiate(optionPrefab, optionParent);
                UIChoiceOption uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceOption>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.SetText(character.GetCombatName());
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(character); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(character); });

                if (choiceIndex == 0) { SoftChooseCharacter(character); }
                choiceIndex++;
            }
            SetUpChoiceOptions();
        }

        private void ChooseCharacter(CombatParticipant character)
        {
            // No actions currently available in character choice -- replace to SoftChoose
            SoftChooseCharacter(character);
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
            StatField levelFieldObject = Instantiate(statFieldPrefab, leftStatParent);
            levelFieldObject.Setup("Level", character.GetLevel());
        }

        private void GenerateHPAP(CombatParticipant character)
        {
            StatField hpFieldObject = Instantiate(statFieldPrefab, rightStatParent);
            hpFieldObject.Setup("HP", character.GetHP(), character.GetMaxHP());

            StatField apFieldObject = Instantiate(statFieldPrefab, rightStatParent);
            apFieldObject.Setup("AP", character.GetAP(), character.GetMaxAP());
        }

        private void GenerateSkillStats(CombatParticipant character)
        {
            Array skillStats = Enum.GetValues(typeof(SkillStat));
            foreach (SkillStat skillStat in skillStats)
            {
                if (Enum.TryParse(skillStat.ToString(), out Stat stat))
                {
                    StatField statField = Instantiate(statFieldPrefab, leftStatParent);
                    float statValue = character.GetStat(stat);
                    statField.Setup(stat, statValue);
                }
            }
        }
    }
}