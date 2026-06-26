using System;
using System.Collections.Generic;

namespace Frankie.Stats
{
    [Serializable]
    public class PartySerializableSaveData
    {
        public readonly List<string> partyCharacterNames;
        public readonly List<string> unlockedCharacterNames;
#pragma warning disable UAC1009
        // Unity serialization error, but serialization is OK by Newtonsoft
        public readonly Dictionary<string, SceneParentReferencePair> worldNPCNameLookup;
#pragma warning restore UAC1009

        public PartySerializableSaveData(List<string> partyCharacterNames, List<string> unlockedCharacterNames, Dictionary<string, SceneParentReferencePair> worldNPCNameLookup)
        {
            this.partyCharacterNames = partyCharacterNames;
            this.partyCharacterNames ??= new List<string>();
            
            this.unlockedCharacterNames = unlockedCharacterNames;
            this.unlockedCharacterNames ??= new List<string>();
            
            this.worldNPCNameLookup = worldNPCNameLookup;
            this.worldNPCNameLookup ??= new Dictionary<string, SceneParentReferencePair>();
        }
    }
}
