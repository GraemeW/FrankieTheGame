using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Combat;
using Frankie.Combat.UI;
using Frankie.Utils.Localization;

namespace Frankie.Stats.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class StatusBox : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedExperienceFlavourText;

        // State
        private readonly StatusBoxModel statusBoxModel = new();
        private CombatParticipantModel selectedCharacter;
        private readonly Dictionary<CombatParticipant, CombatParticipantModel> characterModels = new();

        // UIBox Configuration
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var statusBoxConfiguration = new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            statusBoxConfiguration.TrySet(UIBoxState.Default, new UIBoxStateBehaviour(setupChoiceOptions: ReconcileChoiceOptions));
            return statusBoxConfiguration;
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            GetComponent<UIToolkitMenuView>().SetDataSource(statusBoxModel);
        }

        protected override void StartTriggered()
        {
            statusBoxModel.experienceFlavourText = localizedExperienceFlavourText.GetSafeLocalizedString();
        }

        protected override void DestroyTriggered()
        {
            SetSelectedCharacter(null);
            foreach (CombatParticipantModel characterModel in characterModels.Values) { characterModel.Dispose(); }
            characterModels.Clear();
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedExperienceFlavourText.TableEntryReference,
            };
        }
        #endregion

        #region PublicMethods
        public void Setup(PartyCombatConduit partyCombatConduit)
        {
            var partySelector = new PartySelector(partyCombatConduit, AddNonDestroyChoiceOption, SoftChooseCharacter, SoftChooseCharacter); // No Actions for Choosing on StatusBox (informational only)
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants().Where(character => character != null)) { GetOrBuildCharacterModel(character); }
            
            SoftChooseCharacter(partySelector.firstCharacter);
            ReconcileChoiceOptions();
        }
        #endregion

        #region PrivateMethods
        private void SoftChooseCharacter(CombatParticipant character)
        {
            if (character == null || character == selectedCharacter?.combatParticipant) { return; }
            CombatParticipantModel characterModel = GetOrBuildCharacterModel(character);
            SetSelectedCharacter(characterModel);
            BuildStatSheet();
        }
        
        private CombatParticipantModel GetOrBuildCharacterModel(CombatParticipant character)
        {
            if (characterModels.TryGetValue(character, out CombatParticipantModel characterModel)) { return characterModel; }
            
            characterModel = new CombatParticipantModel(character);
            characterModels[character] = characterModel;
            return characterModel;
        }
        
        private void SetSelectedCharacter(CombatParticipantModel characterModel)
        {
            if (selectedCharacter != null) { selectedCharacter.propertyChanged -= HandleSelectedCharacterChanged; }
            selectedCharacter = characterModel;
            if (selectedCharacter != null) { selectedCharacter.propertyChanged += HandleSelectedCharacterChanged; }
        }

        private void HandleSelectedCharacterChanged(object sender, BindablePropertyChangedEventArgs propertyChangedEventArgs) => BuildStatSheet();

        private void BuildStatSheet()
        {
            if (selectedCharacter == null) { return; }
            
            statusBoxModel.characterName = selectedCharacter.characterName;
            statusBoxModel.experienceToLevelText = selectedCharacter.experienceToLevel.ToString(CultureInfo.InvariantCulture);

            var skillStats = new List<StatLine> { CreateStatLine(Stat.InitialLevel, selectedCharacter.level) };
            skillStats.AddRange(SkillStatAttribute.GetSkillStats().Select(skillStat => CreateStatLine(skillStat, selectedCharacter.GetStat(skillStat))));
            statusBoxModel.skillStats = skillStats;

            statusBoxModel.vitalStats = new List<StatLine>
            {
                CreateStatLine(Stat.HP, selectedCharacter.hp, selectedCharacter.maxHP),
                CreateStatLine(Stat.AP, selectedCharacter.ap, selectedCharacter.maxAP),
            };
        }

        private static StatLine CreateStatLine(Stat stat, float value)
        {
            return new StatLine(LocalizationNames.GetLocalizedName(stat), Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture));
        }

        private static StatLine CreateStatLine(Stat stat, float numerator, float denominator)
        {
            return new StatLine(LocalizationNames.GetLocalizedName(stat), $"{Mathf.RoundToInt(numerator)}/{Mathf.RoundToInt(denominator)}");
        }
        #endregion
    }
}
