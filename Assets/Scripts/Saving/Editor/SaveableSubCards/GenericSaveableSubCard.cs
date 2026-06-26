using UnityEngine.UIElements;

namespace Frankie.Saving.Editor
{
    public class GenericSaveableSubCard : SaveableSubCardData
    {
        public GenericSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            subCardView.Add(new Label("SubCardView not implemented"));
        }
    }
}
