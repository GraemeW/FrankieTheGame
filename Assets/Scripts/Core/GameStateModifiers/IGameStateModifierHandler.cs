using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frankie.Core.GameStateModifiers
{
    public interface IGameStateModifierHandler : ISerializationCallbackReceiver
    {
        // CRITICAL NOTE ON CONFIGURATION ::
        // This interface can ONLY be used for classes derived from MonoBehaviours
        // Then, due to the nature MonoBehaviour event functions, you must:
        // 1 - Add [ExecuteInEditMode] attribute to the class
        // 2 - Include `IGameStateModifierHandler.TriggerOnDestroy(this)` to the OnDestroy() method
        
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

        public static void TriggerOnDestroy(IGameStateModifierHandler gameStateModifierHandler)
        {
            if (!gameStateModifierHandler.IsStandardEditorState()) { return; }
            
            gameStateModifierHandler.RemoveSelfFromGameStateModifiers();
        }
        #endregion
        
        #region InterfaceMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (!IsStandardEditorState()) { return; }
            
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
        private bool IsStandardEditorState()
        {
            if (gameObject == null) { return false; } // Avoid calls due to mis-configuration
            if (!Application.isEditor) { return false; } // Avoid calls outside editor
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) { return false; } // Avoid calls due to play mode start/stop
            if (!gameObject.scene.isLoaded) { return false; } // Avoid calls due to scene changes
            if (EditorUtility.IsPersistent(gameObject)) { return false; } // Avoid calls due to prefab deletion

            return true;
        }
        
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
        
        private void RemoveSelfFromGameStateModifiers()
        {
            foreach (GameStateModifier gameStateModifier in GetGameStateModifiers())
            {
                if (gameStateModifier == null) { continue; }
                gameStateModifier.RemoveGameStateModifierHandler(handlerGUID);
            }
        }
        #endregion
    }
}
