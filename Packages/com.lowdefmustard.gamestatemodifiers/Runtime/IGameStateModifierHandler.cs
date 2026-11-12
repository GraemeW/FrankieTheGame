using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
using LowDefMustard.Utils;

namespace LowDefMustard.GameStateModifiers
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
        public List<string> gameStateModifierGUIDs { get; set; } // Must include explicit backing field in implementation
        
        public static void TriggerOnDestroy(IGameStateModifierHandler gameStateModifierHandler)
        {
#if UNITY_EDITOR
            if (gameStateModifierHandler is not MonoBehaviour monoBehaviour) { return; }
            if (!EditorStateCheck.IsStandardEditorState(monoBehaviour.gameObject)) { return; }
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
        
        public static void RecordHandlerModifications(IGameStateModifierHandler gameStateModifierHandler)
        {
#if UNITY_EDITOR
            // Note:  Direct field writes on a prefab instance are dropped on scene save unless recorded as overrides
            //  - this is NOT safe to call from within OnBeforeSerialize -> so only use this method when deferring / calling after
            if (gameStateModifierHandler is not Component component || component == null) { return; }
            if (!EditorStateCheck.IsStandardEditorState(component.gameObject)) { return; }
            
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) { PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
#endif
        }
        #endregion

        #region InterfaceMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            var component = this as Component;
            if (component == null) { return;} // Avoid gameObject null reference for OnBeforeSerialize() in isolation mode
            if (!EditorStateCheck.IsStandardEditorState(gameObject)) { return; }
            if (!ReconcileGameStateModifiers()) { return; }
            
            EditorApplication.delayCall += () => RecordHandlerModifications(this); // Must delay call, NOT SAFE to call within OnBeforeSerialize()
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
        
#if UNITY_EDITOR
        #region EditorMethods
        // Note:  Internal methods below for test visibility
        
        public IList<GameStateModifier> GetGameStateModifiers();

        public bool ReconcileGameStateModifiers(bool cleanDanglingModifierData = true)
        {
            LinkedHandlerRecord linkedHandlerRecord = MakeLinkedHandlerRecord();
            
            int newModifierListHashCheck = GetModifierListHashCheck(linkedHandlerRecord.zoneName, linkedHandlerRecord.gameObjectName, linkedHandlerRecord.parentObjectName, linkedHandlerRecord.handlerTypeName);
            if (modifierListHashCheck == newModifierListHashCheck) { return false; }
            modifierListHashCheck = newModifierListHashCheck;
            
            // Skip auto-remove on scene failed to be found (serialization quirks)
            var newGameStateModifierGUIDs = AddUpdateGameStateModifiers(linkedHandlerRecord, false, cleanDanglingModifierData);
            RemoveStaleGameStateModifiers(newGameStateModifierGUIDs, false, cleanDanglingModifierData);
            return true;
        }

        internal LinkedHandlerRecord MakeLinkedHandlerRecord()
        {
            // Note: Must ensure zoneName == sceneName
            string zoneName = SceneManager.GetActiveScene().name;
            string gameObjectName = gameObject != null ? gameObject.name : "";
            string parentObjectName = gameObject.transform.parent != null ? gameObject.transform.parent.name : "";
            if (string.IsNullOrWhiteSpace(handlerGUID)) { handlerGUID = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture); }
            
            return new LinkedHandlerRecord(zoneName, gameObjectName, parentObjectName, handlerGUID, GetType().Name);
        }
        
        internal List<string> AddUpdateGameStateModifiers(LinkedHandlerRecord linkedHandlerRecord, bool removeOnSceneCheck = true, bool cleanDanglingModifierData = true)
        {
            hasGameStateModifiers = false;
            var newGameStateModifierGUIDs = new List<string>();
            foreach (GameStateModifier gameStateModifier in GetGameStateModifiers())
            {
                if (gameStateModifier == null) { continue; }
                gameStateModifier.AddOrUpdateGameStateModifierHandler(linkedHandlerRecord);
                if (cleanDanglingModifierData) { gameStateModifier.CleanDanglingModifierHandlerData(removeOnSceneCheck); }
                newGameStateModifierGUIDs.Add(gameStateModifier.GetGUID());
                hasGameStateModifiers = true;
            }

            return newGameStateModifierGUIDs;
        }
        
        internal void RemoveStaleGameStateModifiers(List<string> newGameStateModifierGUIDs, bool removeOnSceneCheck = true, bool cleanDanglingModifierData = true)
        {
            if (gameStateModifierGUIDs == null) { gameStateModifierGUIDs = newGameStateModifierGUIDs; }
            else
            {
                List<string> missingGUIDs = gameStateModifierGUIDs.Except(newGameStateModifierGUIDs).ToList();
                gameStateModifierGUIDs.Clear();
                gameStateModifierGUIDs.AddRange(newGameStateModifierGUIDs);
                if (!cleanDanglingModifierData) { return; }

                foreach (GameStateModifier gameStateModifier in missingGUIDs.Select(GameStateModifier.GetGameStateModifier).Where(gameStateModifier => gameStateModifier != null))
                {
                    gameStateModifier.CleanDanglingModifierHandlerData(removeOnSceneCheck);
                }
            }
        }
        
        internal int GetModifierListHashCheck(string zoneName, string gameObjectName, string parentObjectName, string handlerTypeName)
        {
            uint hash = StableHash.GetOffsetBasis();
            hash = StableHash.Add(hash, zoneName);
            hash = StableHash.Add(hash, gameObjectName);
            hash = StableHash.Add(hash, parentObjectName);
            hash = StableHash.Add(hash, handlerTypeName);
            hash = StableHash.Add(hash, handlerGUID);
            foreach (GameStateModifier gameStateModifier in GetGameStateModifiers())
            {
                hash = StableHash.Add(hash, gameStateModifier != null ? gameStateModifier.GetGUID() : null);
            }
            return unchecked((int)hash);
        }
        
        internal void RemoveSelfFromGameStateModifiers()
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
