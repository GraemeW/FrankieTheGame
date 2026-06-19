using System.Collections.Generic;
using System.Linq;
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

        public SaveableEntityCardData(SaveableEntity saveableEntity, JObject cachedSaveState)
        {
            this.saveableEntity = saveableEntity;

            saveableEntityStateDict = new JObject();
            if (cachedSaveState != null && cachedSaveState.TryGetValue(saveableEntity.GetUniqueIdentifier(), out JToken saveableEntityState))
            {
                SaveableEntity.TryGetStateDictionary(saveableEntityState, out JObject setSaveableEntityStateDict);
                saveableEntityStateDict = setSaveableEntityStateDict;
            }
            
            entityName = saveableEntity.gameObject.name;
            entityID = saveableEntity.GetUniqueIdentifier();
            if (saveableEntity.transform.parent != null) { entityName = $"{saveableEntity.transform.parent.gameObject.name}/{entityName}"; }
            
            foreach (ISaveable saveable in saveableEntity.GetSaveableComponents())
            {
                string typeString = saveable.GetType().ToString();
                SaveState saveState = null;
                if (saveableEntityStateDict != null && saveableEntityStateDict.ContainsKey(typeString)) { saveState = saveableEntityStateDict[typeString]?.ToObject<SaveState>(); }
                subCards[typeString] = SaveableSubCardData.CreateTypeSpecificSubCard(saveable, saveState);
            }
        }

        #region Getters
        public bool TryGetSaveableSubCardData<T>(out SaveableSubCardData matchTypeSubCardData)
        {
            matchTypeSubCardData = null;
            foreach (SaveableSubCardData subCardData in subCards.Values.Where(subCardData => subCardData.saveable is T))
            {
                matchTypeSubCardData = subCardData;
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
        
        public void UpdateStateDict(JObject updatedStateDict)
        {
            if (updatedStateDict == null) { return; }
            saveableEntityStateDict = updatedStateDict;
        }
        
        public void ClearStateDict()
        {
            saveableEntityStateDict = new JObject();
        }
        #endregion
    }
}
