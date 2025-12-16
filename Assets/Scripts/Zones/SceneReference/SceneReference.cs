using UnityEngine;

namespace Frankie.ZoneManagement
{
    [System.Serializable]
    public struct SceneReference
    {
        [SerializeField]
        private Object sceneAsset;
        // sceneAsset used as part of Editor Property Drawer
        // Do NOT delete, even if warning messages encourage you to do so

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
