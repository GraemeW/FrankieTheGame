using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

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
        
        public List<ISaveableBase> GetSaveableComponents() => GetComponents<ISaveableBase>().ToList();
        
        public static bool TryGetStateDictionary(JToken state, out JObject stateDictionary)
        {
            stateDictionary = null;
            if (state == null) { return false; }

            stateDictionary = state.ToObject<JObject>();
            if (stateDictionary == null) { Debug.LogError("Malformed data in save file"); return false; }
            return true;
        }
        
        public JToken CaptureState(JToken existingTokenState, bool onlyCorePlayerState = false)
        {
            JToken updatedTokenState = existingTokenState ?? new JObject();
            foreach (ISaveableBase saveable in GetComponents<ISaveableBase>())
            {
                // Core Player State captures, e.g. for GameOver saves (skip saving position, etc.)
                if (onlyCorePlayerState && !saveable.IsCorePlayerState()) { continue; }
                
                updatedTokenState[saveable.GetType().ToString()] = JToken.FromObject(saveable.CaptureState());
            }
            return updatedTokenState;
        }

        public void RestoreState(JToken state, LoadPriority loadPriority)
        {
            if (!TryGetStateDictionary(state, out JObject stateDictionary)) { return; }

            foreach (ISaveableBase saveable in GetComponents<ISaveableBase>())
            {
                var typeString = saveable.GetType().ToString();
                if (!stateDictionary.ContainsKey(typeString)) continue;
                var saveState = stateDictionary[typeString]?.ToObject<SaveState>();
                if (saveState == null) { return; }

                if (saveState.GetLoadPriority() == loadPriority)
                {
                    saveable.RestoreState(saveState);
                }
            }
        }
        
        public static JObject ManualCaptureSaveState(JObject existingTokenState, SaveState updatedSaveState, string typeString)
        {
            JObject updatedTokenState = existingTokenState ?? new JObject();
            updatedTokenState[typeString] = JToken.FromObject(updatedSaveState);
            return updatedTokenState;
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
