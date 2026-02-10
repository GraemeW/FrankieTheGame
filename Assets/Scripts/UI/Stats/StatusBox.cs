using System;
using UnityEngine;
using TMPro;
using Frankie.Combat;
using Frankie.Utils.UI;

namespace Frankie.Stats.UI
{
    public class StatusBox : UIBox
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] private TextMeshProUGUI selectedCharacterNameField;
        [SerializeField] private TextMeshProUGUI experienceToLevel;
        [Header("Parents")]
        [SerializeField] private Transform leftStatParent;
        [SerializeField] private Transform rightStatParent;
        [Header("Prefabs")]
        [SerializeField] private StatField statFieldPrefab;

        // State
        private CombatParticipant selectedCharacter;

        public void Setup(PartyCombatConduit partyCombatConduit)
        {
            int choiceIndex = 0;
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants())
            {
                GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
                UIChoiceButton uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
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
            if (character == selectedCharacter) return;
            
            OnUIBoxModified(UIBoxModifiedType.ItemSelected, true);
            selectedCharacter = character;
            CleanUpOldStats();

            selectedCharacterNameField.text = selectedCharacter.GetCombatName();
            experienceToLevel.text = selectedCharacter.GetComponent<Experience>().GetExperienceRequiredToLevel().ToString();

            GenerateLevel(character);
            GenerateHPAP(character);
            GenerateSkillStats(character);
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
                if (!Enum.TryParse(skillStat.ToString(), out Stat stat)) continue;
                
                StatField statField = Instantiate(statFieldPrefab, leftStatParent);
                float statValue = character.GetStat(stat);
                statField.Setup(stat, statValue);
            }
        }
    }
}
