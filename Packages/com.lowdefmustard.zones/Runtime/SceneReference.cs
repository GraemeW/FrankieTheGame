using UnityEngine;
using LowDefMustard.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LowDefMustard.Zones
{
    [System.Serializable]
    public struct SceneReference
    {
        // Note:  Internal fields for test visibility
        
        [SerializeField]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        // sceneAsset used as part of Editor Property Drawer
        // Do NOT delete, even if warning messages encourage you to do so
        internal Object sceneAsset;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        [SerializeField] private string sceneName;
        [SerializeField] internal string scenePath;

        public SceneReference(string sceneName)
        {
            sceneAsset = null;
            scenePath = null;
            this.sceneName = sceneName;
        }

        // ReSharper disable once InconsistentNaming
        public string SceneName
        {
            get => sceneName;
            set
            {
                if (string.Equals(sceneName, value)) return;
                sceneAsset = null;
                scenePath = null;
                sceneName = value;
            }
        }
        
        public bool IsSet() => !string.IsNullOrWhiteSpace(sceneName);

        public string GetScenePath()
        {
            if (!string.IsNullOrWhiteSpace(scenePath)) { return scenePath; }
            
#if UNITY_EDITOR
            var trySceneAsset = sceneAsset as SceneAsset;
            if (sceneAsset != null) { return AssetDatabase.GetAssetPath(trySceneAsset); }
#endif
            LDMLog.Warning(LDMLogCategory.Zones, $"Scene path for '{sceneName}' is not configured. Re-link the scene reference in the editor.");
            return string.Empty;
        }
        
        public static implicit operator string(SceneReference? sceneReference)
        {
            return sceneReference?.sceneName;
        }

        public static implicit operator SceneReference(string sceneName)
        {
            return new SceneReference(sceneName);
        }
    }
}
