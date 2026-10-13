using System;
using System.Collections.Generic;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Stats.UI
{
    public sealed class StatusBoxModel : BindableModel
    {
        // State
        private string internalCharacterName = "";
        private string internalExperienceToLevelText = "";
        private string internalExperienceFlavourText = "";
        private IReadOnlyList<StatLine> internalSkillStats = Array.Empty<StatLine>();
        private IReadOnlyList<StatLine> internalVitalStats = Array.Empty<StatLine>();

        [CreateProperty] public string characterName
        {
            get => internalCharacterName;
            set => SetProperty(ref internalCharacterName, value);
        }

        [CreateProperty] public string experienceToLevelText
        {
            get => internalExperienceToLevelText;
            set => SetProperty(ref internalExperienceToLevelText, value);
        }

        [CreateProperty] public string experienceFlavourText
        {
            get => internalExperienceFlavourText;
            set => SetProperty(ref internalExperienceFlavourText, value);
        }

        [CreateProperty] public IReadOnlyList<StatLine> skillStats // Level + skill stats (left column)
        {
            get => internalSkillStats;
            set => SetProperty(ref internalSkillStats, value);
        }

        [CreateProperty] public IReadOnlyList<StatLine> vitalStats // HP/AP (right column)
        {
            get => internalVitalStats;
            set => SetProperty(ref internalVitalStats, value);
        }
    }
}
