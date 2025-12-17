using UnityEngine;

namespace Frankie.ZoneManagement
{
    [System.Serializable]
    public struct SceneReference
    {
        [SerializeField]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        // sceneAsset used as part of Editor Property Drawer
        // Do NOT delete, even if warning messages encourage you to do so
        private Object sceneAsset;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        [SerializeField]
        private string sceneName;

        public SceneReference(string sceneName)
        {
            sceneAsset = null;
            this.sceneName = sceneName;
        }

        public string SceneName
        {
            get => sceneName;
            set
            {
                if (string.Equals(sceneName, value)) return;
                sceneAsset = null;
                sceneName = value;
            }
        }
        
        public bool IsSet() => !string.IsNullOrWhiteSpace(sceneName);
        
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
