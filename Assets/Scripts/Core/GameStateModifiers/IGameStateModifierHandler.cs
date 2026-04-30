using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Frankie.Utils;

namespace Frankie.Core.GameStateModifiers
{
    public interface IGameStateModifierHandler : ISerializationCallbackReceiver
    {
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        // This interface can ONLY be used for classes derived from MonoBehaviours
        //
        // Due to the nature MonoBehaviour event functions, the following must be manually configured:
        // 1 - Add [ExecuteInEditMode] attribute to the class
        // 2 - Include `IGameStateModifierHandler.TriggerOnDestroy(this)` to the OnDestroy() method
        // 3 - Include `IGameStateModifierHandler.TriggerOnGizmos(this)` to the OnGizmos() method
        //
        // Additionally, when implementing properties, it is required to set up with explicitly defined serialized backing fields.
        // See:  QuestHandler implementation as an example
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        
        #region ConstGizmoProperties
        private const float _gizmoStarSize = 0.15f;
        private const float _gizmoYOffset = 0.05f;
        private static readonly Color _gizmosStarColour = Color.yellow;
        private static readonly Color _gizmosCircleColour = Color.red;
        private const float _gizmosLineThickness = 3.0f;
        private const int _gizmoStarPoints = 5;
        private const float _gizmoTwoPI = Mathf.PI * 2f;
        #endregion
        
        #region StandardPropertiesAndMethods
        public GameObject gameObject { get; } // Don't need to define, auto-inherits as long as hooked to MonoBehaviour
        public string handlerGUID { get; set; } // Must include explicit backing field in implementation
        public int modifierListHashCheck { get; set; } // Must include explicit backing field in implementation
        public bool hasGameStateModifiers { get; set; } // Must include explicit backing field in implementation
        
        
        public static void TriggerOnDestroy(IGameStateModifierHandler gameStateModifierHandler)
        {
#if UNITY_EDITOR
            if (gameStateModifierHandler is not MonoBehaviour monoBehaviour) { return; }
            if (!FrankieNonEditorEditorTools.IsStandardEditorState(monoBehaviour.gameObject)) { return; }
            gameStateModifierHandler.RemoveSelfFromGameStateModifiers();
#endif
        }
        
        public static void TriggerOnGizmos(IGameStateModifierHandler gameStateModifierHandler)
        {
#if UNITY_EDITOR
            if (gameStateModifierHandler is not { hasGameStateModifiers: true }) { return; }
            GameObject gameObject = gameStateModifierHandler.gameObject;
            if (gameObject == null) { return; }
            
            Vector3 position = gameObject.transform.position;
            if (gameObject.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                position = spriteRenderer.bounds.center;
            }
            
            DrawGizmoStar(position, gameObject.transform.forward);
#endif
        }
        #endregion

        #region InterfaceMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            var component = this as Component;
            if (component == null) { return;} // Avoid gameObject null reference for OnBeforeSerialize() in isolation mode
            if (!FrankieNonEditorEditorTools.IsStandardEditorState(gameObject)) { return; }
            
            ZoneToGameObjectLinkData zoneToGameObjectLinkData = MakeZoneToGameObjectLinkData();
            
            // Check for changes to asset
            int newModifierListHashCheck = GetModifierListHashCheck(zoneToGameObjectLinkData.zoneName, zoneToGameObjectLinkData.gameObjectName);
            if (modifierListHashCheck == newModifierListHashCheck) { return; }
            modifierListHashCheck = newModifierListHashCheck;

            hasGameStateModifiers = false;
            foreach (GameStateModifier gameStateModifier in GetGameStateModifiers())
            {
                if (gameStateModifier == null) { continue; }
                gameStateModifier.AddOrUpdateGameStateModifierHandler(zoneToGameObjectLinkData);
                gameStateModifier.CleanDanglingModifierHandlerData();
                hasGameStateModifiers = true;
            }
            
            ForceSerializeGameObject();
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
        
#if UNITY_EDITOR
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
        
        #region EditorMethods
        public IList<GameStateModifier> GetGameStateModifiers();

        private void ForceSerializeGameObject()
        {
            if (gameObject == null) { return; }
            SerializedObject serializedObject = new SerializedObject(gameObject);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

        private static void DrawGizmoStar(Vector3 centrePosition, Vector3 normalForward)
        {
            Vector3 position = centrePosition + Vector3.up * _gizmoYOffset;
            float outerRadius = _gizmoStarSize * 0.5f;
            float innerRadius = outerRadius * 0.382f; // golden ratio approximation for a natural star
            float startAngle = -Mathf.PI / 2f;
            float angleStep = _gizmoTwoPI / (_gizmoStarPoints * 2);
            
            Vector3[] vertices = new Vector3[_gizmoStarPoints * 2];
            for (int i = 0; i < _gizmoStarPoints * 2; i++)
            {
                float angle = startAngle + i * angleStep;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                vertices[i] = position + new Vector3( Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f );
            }
            
            // Outer Circle
            Handles.color = _gizmosCircleColour;
            Handles.DrawSolidDisc(position, normalForward, _gizmoStarSize * 0.6f);
            
            // Star
            Handles.color = _gizmosStarColour;
            for (int i = 0; i < _gizmoStarPoints * 2; i++)
            {
                Handles.DrawLine(vertices[i], vertices[(i + 1) % (_gizmoStarPoints * 2)], _gizmosLineThickness);
            }
        }
        #endregion
#endif
    }
}
