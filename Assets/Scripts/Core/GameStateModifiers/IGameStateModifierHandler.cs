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
        public string gameStateGUID { get; set; }
        public int modifierListHashCheck { get; set; }
        #endregion

        #region Methods
        public IList<GameStateModifier> GetGameStateModifiers();
        #endregion
        
        #region StaticMethods
        public static int GetModifierListHashCheck(IGameStateModifierHandler gameStateModifierHandler)
        {
            if (gameStateModifierHandler == null) { return 0; }
            
            var hash = new HashCode();
            foreach (GameStateModifier gameStateModifier in gameStateModifierHandler.GetGameStateModifiers())
            {
                hash.Add(gameStateModifier);
            }
            return hash.ToHashCode();
        }
        #endregion
        
        #region InterfaceMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // Need to check on this because serialization code can enter a state where Mono's link breaks
            if (this == null) {  return; }
            if (gameObject == null) { return; }
            
            // Avoid GUID generation and link set-up for prefabs
            if (EditorUtility.IsPersistent(gameObject)) { return; }

            int newModifierListHashCheck = GetModifierListHashCheck(this);
            if (modifierListHashCheck == newModifierListHashCheck) { return; }
            modifierListHashCheck = newModifierListHashCheck;
            
            // Generate and save a new UUID if this is blank
            if (string.IsNullOrWhiteSpace(gameStateGUID)) { gameStateGUID = Guid.NewGuid().ToString(); }
            
            // Note: Must ensure zoneName == sceneName
            // A warning is present in Zone class' SerializationCallbackReceiver if this is not true
            string zoneName = SceneManager.GetActiveScene().name;
            string gameObjectName = gameObject != null ? gameObject.name : "";
            
            var zoneToGameObjectLinkData = new ZoneToGameObjectLinkData(zoneName, gameObjectName, gameStateGUID);
            foreach (GameStateModifier gameStateModifier in GetGameStateModifiers())
            {
                if (gameStateModifier == null) { continue; }
                gameStateModifier.AddOrUpdateGameStateModifierHandler(zoneToGameObjectLinkData);
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
