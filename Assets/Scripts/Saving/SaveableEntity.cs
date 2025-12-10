using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

namespace Frankie.Saving
{
    [ExecuteAlways]
    public class SaveableEntity : MonoBehaviour
    {
        // Tunables
        [SerializeField] private string uniqueIdentifier = "";
        private static readonly Dictionary<string, SaveableEntity> _globalLookupForEditorDebug = new();
        
        // Constants
        private const string _uniquePropertyRef = "uniqueIdentifier";

        public string GetUniqueIdentifier()
        {
            if (string.IsNullOrWhiteSpace(uniqueIdentifier)) { uniqueIdentifier = Guid.NewGuid().ToString(); }
            return uniqueIdentifier;
        }
        
        public JToken CaptureState()
        {
            var state = new JObject();
            foreach (ISaveable saveable in GetComponents<ISaveable>())
            {
                state[saveable.GetType().ToString()] = JToken.FromObject(saveable.CaptureState());
            }
            return state;
        }

        public void RestoreState(JToken state, LoadPriority loadPriority)
        {
            if (state == null) { return; }

            var stateDict = state.ToObject<JObject>();
            if (stateDict == null) { Debug.LogError("Malformed data in save file"); return; }

            foreach (ISaveable saveable in GetComponents<ISaveable>())
            {
                var typeString = saveable.GetType().ToString();
                if (!stateDict.ContainsKey(typeString)) continue;
                var saveState = stateDict[typeString]?.ToObject<SaveState>();
                if (saveState == null) { return; }

                if (saveState.GetLoadPriority() == loadPriority)
                {
                    saveable.RestoreState(saveState);
                }
            }
        }
        
#if UNITY_EDITOR
        private void Update()
        {
            if (Application.IsPlaying(gameObject)) { return; }
            if (string.IsNullOrEmpty(gameObject.scene.path)) { return; }
            if (StageUtility.GetStage(gameObject) != StageUtility.GetMainStage()) { return; }

            var serializedObject = new SerializedObject(this);
            SerializedProperty property = serializedObject.FindProperty(_uniquePropertyRef);

            if (string.IsNullOrWhiteSpace(uniqueIdentifier) || !IsUnique(property.stringValue))
            {
                if (!IsUnique(property.stringValue)) { Debug.Log($"Warning:  Duplicate GUID identified for {gameObject.name} @ {uniqueIdentifier} -> Generating a new GUID"); }
                property.stringValue = Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
            }

            _globalLookupForEditorDebug[property.stringValue] = this;
        }
        
        private bool IsUnique(string candidate)
        {
            if (!_globalLookupForEditorDebug.TryGetValue(candidate, out SaveableEntity value)) { return true; }
            if (value == this) { return true; }
            if (_globalLookupForEditorDebug[candidate] == null || _globalLookupForEditorDebug[candidate].GetUniqueIdentifier() != candidate)
            {
                _globalLookupForEditorDebug.Remove(candidate);
                return true;
            }
            return false;
        }
#endif
    }
}
