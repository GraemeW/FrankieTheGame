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
        
        #region PrivateMethods
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
        #endregion
        
        #region InterfaceMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (gameObject == null) { return; }
            
            // Avoid GUID generation and link set-up for prefabs
            if (EditorUtility.IsPersistent(gameObject)) { return; }
            
            // Note: Must ensure zoneName == sceneName
            string zoneName = SceneManager.GetActiveScene().name;
            string gameObjectName = gameObject != null ? gameObject.name : "";
            if (string.IsNullOrWhiteSpace(handlerGUID)) { handlerGUID = Guid.NewGuid().ToString(); }
            
            // Check for changes to asset
            int newModifierListHashCheck = GetModifierListHashCheck(zoneName, gameObjectName);
            if (modifierListHashCheck == newModifierListHashCheck) { return; }
            modifierListHashCheck = newModifierListHashCheck;
            
            var zoneToGameObjectLinkData = new ZoneToGameObjectLinkData(zoneName, gameObjectName, handlerGUID);
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
    }
}
