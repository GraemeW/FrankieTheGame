using System;
using System.Collections.Generic;
using System.Globalization;
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
            if (string.IsNullOrWhiteSpace(uniqueIdentifier)) { uniqueIdentifier = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture); }
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

                SaveState saveState = saveable.CaptureState();
                if (saveState == null) { continue; }
                updatedTokenState[saveable.GetType().ToString()] = JToken.FromObject(saveState); // Type ToString does not require CultureInvariant
            }
            return updatedTokenState;
        }

        public void RestoreState(JToken state, LoadPriority loadPriority)
        {
            if (!TryGetStateDictionary(state, out JObject stateDictionary)) { return; }

            foreach ((ISaveableBase saveable, SaveState saveState) in MatchSaveableToState(GetComponents<ISaveableBase>(), stateDictionary))
            {
                if (saveState.GetLoadPriority() == loadPriority)
                {
                    saveable.RestoreState(saveState);
                }
            }
        }

        private static IEnumerable<(ISaveableBase, SaveState)> MatchSaveableToState(IEnumerable<ISaveableBase> saveableEntries, JObject stateDictionary)
        {
            foreach (ISaveableBase saveable in saveableEntries)
            {
                var typeString = saveable.GetType().ToString(); // Type ToString does not require CultureInvariant
                if (!stateDictionary.ContainsKey(typeString)) { continue; }
                var saveState = stateDictionary[typeString]?.ToObject<SaveState>();
                if (saveState == null) { continue; }

                yield return (saveable, saveState);
            }
        }
        
        public static JObject ManualCaptureSaveState(JObject stateDictionary, string typeString, SaveState updatedSaveState)
        {
            JObject updatedStateDictionary = stateDictionary ?? new JObject();
            updatedStateDictionary[typeString] = JToken.FromObject(updatedSaveState);
            return updatedStateDictionary;
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
                property.stringValue = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
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
