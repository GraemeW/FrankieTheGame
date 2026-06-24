using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Frankie.Saving.Editor
{
    public class SaveableEntityCardData
    {
        // Core Data
        public SaveableEntity saveableEntity { get; private set; }
        public JObject saveableEntityStateDict { get; private set; }
        public Dictionary<string, SaveableSubCardData> subCards { get; private set; } = new();
        // Derived Data
        public string entityName { get; private set; }
        public string entityID { get; private set; }

        public SaveableEntityCardData(SaveableEntity saveableEntity, JObject saveableEntityStateDict)
        {
            this.saveableEntity = saveableEntity;
            saveableEntityStateDict ??= new JObject();
            this.saveableEntityStateDict = saveableEntityStateDict;
            
            if (saveableEntity == null) { return; }
            
            entityName = saveableEntity.gameObject.name;
            entityID = saveableEntity.GetUniqueIdentifier();
            if (saveableEntity.transform.parent != null) { entityName = $"{saveableEntity.transform.parent.gameObject.name}/{entityName}"; }
            
            foreach (ISaveableBase saveable in saveableEntity.GetSaveableComponents())
            {
                string typeString = saveable.GetType().ToString();
                SaveState saveState = null;
                if (saveableEntityStateDict.ContainsKey(typeString)) { saveState = saveableEntityStateDict[typeString]?.ToObject<SaveState>(); }
                subCards[typeString] = SaveableSubCardData.CreateTypeSpecificSubCard(saveable, saveState);
            }
        }

        #region Getters
        public bool TryGetSaveableSubCardData<T>(out T matchTypeSubCardData)
        {
            matchTypeSubCardData = default;
            foreach (SaveableSubCardData subCardData in subCards.Values)
            {
                if (subCardData is not T matchedType) { continue; }
                matchTypeSubCardData = matchedType;
                return true;
            }
            return false;
        }

        public bool HasPlayerMoverWithAlteredPosition()
        {
            foreach (SaveableSubCardData subCardData in subCards.Values)
            {
                if (subCardData is not MoverSaveableSubCard moverSaveableSubCard || !moverSaveableSubCard.IsPlayerMoverSubCard()) { continue; }
                if (moverSaveableSubCard.IsSaveStateSynced()) { continue; }
                
                return true;
            }
            return false;
        }
        #endregion
        
        #region Setters
        public void SelfReferenceInSubCards()
        {
            // Call separately because doing so in construction is unsafe
            foreach (SaveableSubCardData subCardData in subCards.Values)
            {
                subCardData.SetSaveableEntityCardData(this);
            }
        }

        public void ResetSaveableSyncFlag()
        {
            foreach (SaveableSubCardData subCardData in subCards.Values)
            {
                subCardData.ResetSyncFlag();
                subCardData.Redraw();
            }
        }
        
        public void UpdateStateDict(JObject updatedStateDict)
        {
            if (updatedStateDict == null) { return; }
            saveableEntityStateDict = updatedStateDict;
        }

        public void UpdateSaveableEntry(string typeString, SaveState updatedSaveState)
        {
            SaveableEntity.ManualCaptureSaveState(saveableEntityStateDict, typeString, updatedSaveState);
        }
        
        public void ClearStateDict()
        {
            saveableEntityStateDict = new JObject();
        }
        #endregion
    }
}
