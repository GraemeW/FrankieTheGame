using System;

namespace LowDefMustard.GameStateModifiers
{
    [Serializable]
    public struct LinkedHandlerRecord : IEquatable<LinkedHandlerRecord>
    {
        public string zoneName;
        public string gameObjectName;
        public string parentObjectName;
        public string handlerGUID;
        public string handlerTypeName;

        public static string GetZoneNameRef() => nameof(zoneName);
        public static string GetGameObjectNameRef() => nameof(gameObjectName);
        public static string GetParentObjectNameRef() => nameof(parentObjectName);
        public static string GetHandlerGUIDRef() => nameof(handlerGUID);
        public static string GetHandlerTypeNameRef() => nameof(handlerTypeName);

        public LinkedHandlerRecord(string zoneName, string gameObjectName, string parentObjectName, string handlerGUID, string handlerTypeName = "")
        {
            this.zoneName = zoneName;
            this.gameObjectName = gameObjectName;
            this.parentObjectName = parentObjectName;
            this.handlerGUID = handlerGUID;
            this.handlerTypeName = handlerTypeName;
        }

        public void UpdateRecord(string setZoneName, string setGameObjectName, string setParentObjectName, string setHandlerTypeName)
        {
            zoneName = setZoneName;
            gameObjectName = setGameObjectName;
            parentObjectName = setParentObjectName;
            handlerTypeName = setHandlerTypeName;
        }
        
        public string GetParentLabelStem() => !string.IsNullOrEmpty(parentObjectName) ? $"{parentObjectName}." : ""; 

        public bool Equals(LinkedHandlerRecord other) => handlerGUID == other.handlerGUID;
        public override bool Equals(object obj) => obj is LinkedHandlerRecord other && Equals(other); 
        public override int GetHashCode() => handlerGUID.GetHashCode();
    }
}
