using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class SkillSelectionCharacterLabel : ModelBoundLabel
    {
        public SkillSelectionCharacterLabel() : base(nameof(SkillSelectionModel.characterName)) { }
    }
}
