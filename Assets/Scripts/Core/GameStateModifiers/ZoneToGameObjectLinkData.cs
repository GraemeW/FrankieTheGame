using System;

namespace Frankie.Core.GameStateModifiers
{
    [Serializable]
    public struct ZoneToGameObjectLinkData : IEquatable<ZoneToGameObjectLinkData>
    {
        public string zoneName;
        public string gameObjectName;
        public string parentObjectName;
        public string guid;

        public static string GetZoneNameRef() => nameof(zoneName);
        public static string GetGameObjectNameRef() => nameof(gameObjectName);
        public static string GetParentObjectNameRef() => nameof(parentObjectName);
        public static string GetGuidRef() => nameof(guid);

        public ZoneToGameObjectLinkData(string zoneName, string gameObjectName, string parentObjectName, string guid)
        {
            this.zoneName = zoneName;
            this.gameObjectName = gameObjectName;
            this.parentObjectName = parentObjectName;
            this.guid = guid;
        }

        public void UpdateRecord(string setZoneName, string setGameObjectName, string setParentObjectName)
        {
            zoneName = setZoneName;
            gameObjectName = setGameObjectName;
            parentObjectName = setParentObjectName;
        }

        public string GetParentLabelStem() => parentObjectName != null ? $"{parentObjectName}." : ""; 

        public bool Equals(ZoneToGameObjectLinkData other) => guid == other.guid;
        public override bool Equals(object obj) => obj is ZoneToGameObjectLinkData other && Equals(other); 
        public override int GetHashCode() => guid.GetHashCode();
    }
}
