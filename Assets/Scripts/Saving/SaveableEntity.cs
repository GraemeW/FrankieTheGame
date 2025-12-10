using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace Frankie.Saving
{
    [ExecuteAlways]
    public class SaveableEntity : MonoBehaviour
    {
        // Tunables
        [SerializeField] private string uniqueIdentifier = "";
        private static readonly Dictionary<string, SaveableEntity> _globalLookup = new();
        
        // Constants
        private const string _uniquePropertyRef = "uniqueIdentifier";

        public string GetUniqueIdentifier() => uniqueIdentifier;
        
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

        private bool IsUnique(string candidate)
        {
            if (!_globalLookup.TryGetValue(candidate, out SaveableEntity value)) { return true; }
            if (value == this) { return true; }
            if (_globalLookup[candidate] == null || _globalLookup[candidate].GetUniqueIdentifier() != candidate)
            {
                _globalLookup.Remove(candidate);
                return true;
            }
            return false;
        }
        
#if UNITY_EDITOR
        private void Update()
        {
            if (Application.IsPlaying(gameObject)) return;
            if (string.IsNullOrEmpty(gameObject.scene.path)) return;

            var serializedObject = new SerializedObject(this);
            SerializedProperty property = serializedObject.FindProperty(_uniquePropertyRef);

            if (string.IsNullOrEmpty(property.stringValue) || !IsUnique(property.stringValue))
            {
                property.stringValue = System.Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
            }

            _globalLookup[property.stringValue] = this;
        }
#endif

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(uniqueIdentifier))
            {
                uniqueIdentifier = System.Guid.NewGuid().ToString();
            }
#endif
        }
    }
}
