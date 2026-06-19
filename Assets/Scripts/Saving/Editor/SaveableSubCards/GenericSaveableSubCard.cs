using UnityEngine.UIElements;

namespace Frankie.Saving.Editor.SaveableSubCards
{
    public class GenericSaveableSubCard : SaveableSubCardData
    {
        public GenericSaveableSubCard(ISaveable saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            subCardView.Add(new Label("SubCardView not implemented"));
        }
    }
}
