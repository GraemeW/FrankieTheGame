using System.Collections.Generic;
using System.Linq;

namespace Frankie.Stats
{
    public class PartySaveData
    {
        public readonly List<CharacterProperties> partyCharacters;
        public readonly HashSet<CharacterProperties> unlockedCharacters;
#pragma warning disable UAC1009
        // Unity serialization error, but serialization is OK by Newtonsoft
        public readonly Dictionary<CharacterProperties, SceneParentReferencePair> worldNPCLookup;
#pragma warning restore UAC1009

        public PartySaveData(List<CharacterProperties> partyCharacters, HashSet<CharacterProperties> unlockedCharacters, Dictionary<CharacterProperties, SceneParentReferencePair> worldNPCLookup)
        {
            this.partyCharacters = partyCharacters;
            this.partyCharacters ??= new List<CharacterProperties>();
            
            this.unlockedCharacters = unlockedCharacters;
            this.unlockedCharacters ??= new HashSet<CharacterProperties>();
            
            this.worldNPCLookup = worldNPCLookup;
            this.worldNPCLookup ??= new Dictionary<CharacterProperties, SceneParentReferencePair>();
        }

        public PartySaveData()
        {
            partyCharacters = new List<CharacterProperties>();
            unlockedCharacters = new HashSet<CharacterProperties>();
            worldNPCLookup = new Dictionary<CharacterProperties, SceneParentReferencePair>();
        }

        public static PartySaveData FromSerializableSaveData(PartySerializableSaveData partySerializableSaveData)
        {
            if (partySerializableSaveData == null) { return new PartySaveData(); }
            
            var worldNPCLookup = new Dictionary<CharacterProperties, SceneParentReferencePair>();
            foreach ((string characterID, SceneParentReferencePair sceneParentReferencePair) in partySerializableSaveData.worldNPCNameLookup ?? new Dictionary<string, SceneParentReferencePair>())
            {
                if (CharacterProperties.TryGetCharacterPropertiesFromName(characterID, out CharacterProperties characterProperties)) { worldNPCLookup[characterProperties] = sceneParentReferencePair; }
            }
            
            return new PartySaveData(CharacterProperties.GetCharacterPropertiesFromNames(partySerializableSaveData.partyCharacterNames), CharacterProperties.GetCharacterPropertiesFromNames(partySerializableSaveData.unlockedCharacterNames).ToHashSet(), worldNPCLookup);
        }

        public PartySerializableSaveData ToSerializableSaveData()
        {
            var worldNPCNameLookup = new Dictionary<string, SceneParentReferencePair>();
            foreach ((CharacterProperties characterProperties, SceneParentReferencePair sceneParentReferencePair) in worldNPCLookup)
            {
                if (characterProperties != null) { worldNPCNameLookup[characterProperties.GetCharacterID()] = sceneParentReferencePair; }
            }
            
            return new PartySerializableSaveData(CharacterProperties.GetCharacterIDs(partyCharacters), CharacterProperties.GetCharacterIDs(unlockedCharacters), worldNPCNameLookup);
        }
    }
}
