using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public class SkillSelectionModel : BindableModel
    {
        // State
        private string internalCharacterName = "";
        private string internalUpSkillText = "";
        private string internalLeftSkillText = "";
        private string internalRightSkillText = "";
        private string internalDownSkillText = "";
        private string internalActiveSkillText = "";
        private bool internalHasActiveSkill = false;

        [CreateProperty] public string characterName
        {
            get => internalCharacterName;
            set => SetProperty(ref internalCharacterName, value);
        }

        [CreateProperty] public string upSkillText
        {
            get => internalUpSkillText;
            set => SetProperty(ref internalUpSkillText, value);
        }

        [CreateProperty] public string leftSkillText
        {
            get => internalLeftSkillText;
            set => SetProperty(ref internalLeftSkillText, value);
        }

        [CreateProperty] public string rightSkillText
        {
            get => internalRightSkillText;
            set => SetProperty(ref internalRightSkillText, value);
        }

        [CreateProperty] public string downSkillText
        {
            get => internalDownSkillText;
            set => SetProperty(ref internalDownSkillText, value);
        }

        [CreateProperty] public string activeSkillText
        {
            get => internalActiveSkillText;
            set => SetProperty(ref internalActiveSkillText, value);
        }

        [CreateProperty] public bool hasActiveSkill
        {
            get => internalHasActiveSkill;
            set => SetProperty(ref internalHasActiveSkill, value);
        }
    }
}
