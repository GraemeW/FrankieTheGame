using System;

namespace Frankie.ZoneManagement
{
    public struct SceneQueueData
    {
        public readonly float delayTime;
        public readonly Action sceneLoadedCallback;
        public readonly Zone zoneOverride;
        public readonly bool useFader;
        
        public SceneQueueData(Zone zoneOverride, Action sceneLoadedCallback, float delayTime, bool useFader)
        {
            this.zoneOverride = zoneOverride;
            this.sceneLoadedCallback = sceneLoadedCallback;
            this.delayTime = delayTime;
            this.useFader = useFader;
        }

        public SceneQueueData(bool useFader)
        {
            this.useFader = useFader;
            zoneOverride = null;
            sceneLoadedCallback = null;
            delayTime = 0f;
        }
    }
}