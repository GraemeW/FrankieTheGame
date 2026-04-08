using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frankie.Core.GameStateModifiers
{
    public interface IGameStateModifierHandler : ISerializationCallbackReceiver
    {
        #region Properties
        public GameObject gameObject { get; } // Don't need to define, auto-inherits as long as hooked to MonoBehaviour
        public string handlerGUID { get; set; }
        public int modifierListHashCheck { get; set; }
        #endregion

        #region CustomComparer
        private static readonly IEqualityComparer<GameStateModifier> _gameStateModifierComparer = new GameStateModifierComparer();
        class GameStateModifierComparer : IEqualityComparer<GameStateModifier>
        {
            public bool Equals(GameStateModifier x, GameStateModifier y)
            {
                if (x == null && y == null) { return true; }
                if (x == null || y == null) { return false; }
                return x.GetGUID() == y.GetGUID();
            }
            public int GetHashCode(GameStateModifier obj) => obj == null ? 0 : obj.GetGUID().GetHashCode();
        }
        #endregion
        
        #region PublicMethods
        public IList<GameStateModifier> GetGameStateModifiers();
        
        #endregion
        
        #region InterfaceMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            // Avoid GUID generation and link set-up for prefabs
            if (gameObject == null) { return; }
            if (!Application.isEditor || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) { return; }
            if (EditorUtility.IsPersistent(gameObject)) { return; }
            
            ZoneToGameObjectLinkData zoneToGameObjectLinkData = MakeZoneToGameObjectLinkData();
            
            // Check for changes to asset
            int newModifierListHashCheck = GetModifierListHashCheck(zoneToGameObjectLinkData.zoneName, zoneToGameObjectLinkData.gameObjectName);
            if (modifierListHashCheck == newModifierListHashCheck) { return; }
            modifierListHashCheck = newModifierListHashCheck;
            
            foreach (GameStateModifier gameStateModifier in GetGameStateModifiers())
            {
                if (gameStateModifier == null) { continue; }
                gameStateModifier.AddOrUpdateGameStateModifierHandler(zoneToGameObjectLinkData);
                gameStateModifier.CleanDanglingModifierHandlerData();
            }
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
        
        #region PrivateMethods
        private ZoneToGameObjectLinkData MakeZoneToGameObjectLinkData()
        {
            // Note: Must ensure zoneName == sceneName
            string zoneName = SceneManager.GetActiveScene().name;
            string gameObjectName = gameObject != null ? gameObject.name : "";
            if (string.IsNullOrWhiteSpace(handlerGUID)) { handlerGUID = Guid.NewGuid().ToString(); }
            
            return new ZoneToGameObjectLinkData(zoneName, gameObjectName, handlerGUID);
        }
        
        private int GetModifierListHashCheck(string zoneName, string gameObjectName)
        {
            var hash = new HashCode();
            hash.Add(zoneName);
            hash.Add(gameObjectName);
            hash.Add(handlerGUID);
            foreach (GameStateModifier gameStateModifier in GetGameStateModifiers())
            {
                hash.Add(gameStateModifier, _gameStateModifierComparer);
            }
            return hash.ToHashCode();
        }
        
        // Option to implement for OnDestroy, but no effective way to discriminate types of destruction
        private void RemoveSelfFromGameStateModifiers()
        {
            if (gameObject == null || EditorUtility.IsPersistent(gameObject)) { return; }
            
            foreach (GameStateModifier gameStateModifier in GetGameStateModifiers())
            {
                if (gameStateModifier == null) { continue; }
                gameStateModifier.RemoveGameStateModifierHandler(handlerGUID);
            }
        }
        #endregion
    }
}
