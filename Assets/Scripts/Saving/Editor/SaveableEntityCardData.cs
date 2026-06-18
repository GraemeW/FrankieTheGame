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

        public SaveableEntityCardData(SaveableEntity saveableEntity, JObject cachedSaveState)
        {
            this.saveableEntity = saveableEntity;
            
            JObject setSaveableEntityStateDict = null;
            if (cachedSaveState != null && cachedSaveState.TryGetValue(saveableEntity.GetUniqueIdentifier(), out JToken saveableEntityState)) { SaveableEntity.TryGetStateDictionary(saveableEntityState, out setSaveableEntityStateDict); }
            saveableEntityStateDict = setSaveableEntityStateDict;
            
            entityName = saveableEntity.gameObject.name;
            entityID = saveableEntity.GetUniqueIdentifier();
            if (saveableEntity.transform.parent != null) { entityName = $"{saveableEntity.transform.parent.gameObject.name}/{entityName}"; }
            
            foreach (ISaveable saveable in saveableEntity.GetSaveableComponents())
            {
                string typeString = saveable.GetType().ToString();
                SaveState saveState = null;
                if (saveableEntityStateDict != null && saveableEntityStateDict.ContainsKey(typeString)) { saveState = saveableEntityStateDict[typeString]?.ToObject<SaveState>(); }
                subCards[typeString] = new SaveableSubCardData(saveable, saveState);
            }
        }
    }
}
