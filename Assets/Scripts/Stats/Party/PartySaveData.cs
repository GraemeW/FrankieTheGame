using System.Collections.Generic;

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
    }
}
