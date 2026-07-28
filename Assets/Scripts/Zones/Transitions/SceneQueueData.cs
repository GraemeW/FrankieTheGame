using System;

namespace Frankie.ZoneManagement
{
    public struct SceneQueueData
    {
        public readonly float delayTime;
        public readonly Action sceneLoadedCallback;
        public readonly bool useFader;
        
        public SceneQueueData(Action sceneLoadedCallback, float delayTime, bool useFader)
        {
            this.sceneLoadedCallback = sceneLoadedCallback;
            this.delayTime = delayTime;
            this.useFader = useFader;
        }

        public SceneQueueData(bool useFader)
        {
            this.useFader = useFader;
            sceneLoadedCallback = null;
            delayTime = 0f;
        }
    }
}