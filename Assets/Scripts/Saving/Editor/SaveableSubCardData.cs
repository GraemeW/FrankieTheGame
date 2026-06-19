using UnityEngine.UIElements;
using Frankie.Inventory;
using Frankie.Saving.Editor.SaveableSubCards;

namespace Frankie.Saving.Editor
{
    public abstract class SaveableSubCardData
    {
        protected SaveableEntityCardData saveableEntityCardData { get; private set; }
        public ISaveable saveable { get; protected set; }
        public SaveState saveState { get; protected set; }

        public static SaveableSubCardData CreateTypeSpecificSubCard(ISaveable saveable, SaveState saveState)
        {
            return saveable switch
            {
                Equipment => new EquipmentSaveableSubCard(saveable, saveState),
                _ => new GenericSaveableSubCard(saveable, saveState)
            };
        }
        
        public abstract void AddEditableFieldsToSubCardView(Box subCardView);
        
        public void SetSaveableEntityCardData(SaveableEntityCardData setSaveableEntityCardData)
        {
            saveableEntityCardData = setSaveableEntityCardData;
        }
    }
}
