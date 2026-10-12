using Unity.Properties;

namespace Frankie.Combat.UI
{
    public sealed class AbilitiesBoxModel : SkillSelectionModel
    {
        // State
        private string internalStatLabel = "";
        private string internalStatText = "";
        private string internalAPCostLabel = "";
        private string internalAPCostText = "";
        private string internalDetailText = "";

        [CreateProperty] public string statLabel
        {
            get => internalStatLabel;
            set => SetProperty(ref internalStatLabel, value);
        }

        [CreateProperty] public string statText
        {
            get => internalStatText;
            set => SetProperty(ref internalStatText, value);
        }

        [CreateProperty] public string apCostLabel
        {
            get => internalAPCostLabel;
            set => SetProperty(ref internalAPCostLabel, value);
        }

        [CreateProperty] public string apCostText
        {
            get => internalAPCostText;
            set => SetProperty(ref internalAPCostText, value);
        }

        [CreateProperty] public string detailText
        {
            get => internalDetailText;
            set => SetProperty(ref internalDetailText, value);
        }
    }
}
