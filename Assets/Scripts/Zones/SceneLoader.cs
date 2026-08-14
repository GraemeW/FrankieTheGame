using LowDefMustard.Zones;

namespace Frankie.Zones
{
    public class SceneLoader : SceneLoaderBase<SceneQueueType>
    {
        protected override bool IsNewGameSceneType(SceneQueueType sceneType) => sceneType == SceneQueueType.New;
        protected override bool IsGameOverSceneType(SceneQueueType sceneType) => sceneType == SceneQueueType.GameOver;
    }
}
