namespace Frankie.Core.GameStateModifiers
{
    [System.Serializable]
    public struct ZoneToGameObjectLinkData
    {
        public string zoneName;
        public string gameObjectName;
        public string guid;

        public static string GetZoneNameRef() => nameof(zoneName);
        public static string GetGameObjectNameRef() => nameof(gameObjectName);
        public static string GetGuidRef() => nameof(guid);

        public ZoneToGameObjectLinkData(string zoneName, string gameObjectName, string guid)
        {
            this.zoneName = zoneName;
            this.gameObjectName = gameObjectName;
            this.guid = guid;
        }

        public void UpdateRecord(string setZoneName, string setGameObjectName)
        {
            // Note:  no update for GUID, since this is the only way to practically identify the record
            zoneName = setZoneName;
            gameObjectName = setGameObjectName;
        }
    }
}
