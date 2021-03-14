using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.SceneManagement
{
    [System.Serializable]
    public struct SceneReference
    {
        [SerializeField]
        private Object sceneAsset;

        [SerializeField]
        private string sceneName;

        public SceneReference(string sceneName)
        {
            sceneAsset = null;
            this.sceneName = sceneName;
        }

        public Object SceneAsset
        {
            get { return sceneAsset; }
        }

        public string SceneName
        {
            get { return sceneName; }
            set
            {
                if (!string.Equals(sceneName, value))
                {
                    sceneAsset = null;
                    sceneName = value;
                }
            }
        }

        public static implicit operator string(SceneReference sceneReference)
        {
            if (sceneReference == null) { return null; }
            return sceneReference.sceneName;
        }

        public static implicit operator SceneReference(string sceneName)
        {
            return new SceneReference(sceneName);
        }
    }

}
