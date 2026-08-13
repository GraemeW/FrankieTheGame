using System;
using LowDefMustard.Utils;

namespace LowDefMustard.Zones
{
    [Serializable]
    public class ZoneSceneTypeLookup<TSceneType> : EnumKeyedCollection<TSceneType, Zone> where TSceneType : struct, Enum { }
}
