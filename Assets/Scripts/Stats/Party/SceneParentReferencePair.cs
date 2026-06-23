using System;

namespace Frankie.Stats
{
    [Serializable]
    public struct SceneParentReferencePair
    {
        public string sceneName;
        public string parentName;

        public SceneParentReferencePair(string sceneName, string parentName)
        {
            this.sceneName = sceneName;
            this.parentName = parentName;
        }
    }
}
