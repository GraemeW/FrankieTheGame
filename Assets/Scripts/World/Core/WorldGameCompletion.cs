using UnityEngine;
using LowDefMustard.Core;
using LowDefMustard.Zones;
using Frankie.Core;
using Frankie.Zones;

namespace Frankie.World
{
    public class WorldGameCompletion : MonoBehaviour
    {
        #region PublicMethods
        public void TriggerGameOver(PlayerStateMachine playerStateMachine)
        {
            LDMLog.Important(LDMLogCategory.World, "Game over.");
            playerStateMachine.EnterCutscene();
            SceneLoader.QueueScene(SceneQueueType.GameOver, new SceneQueueData(true));
        }

        public void TriggerGameWin(PlayerStateMachine playerStateMachine)
        {
            LDMLog.Important(LDMLogCategory.World, "Game won.");
            playerStateMachine.EnterCutscene();
            SceneLoader.QueueScene(SceneQueueType.GameWin, new SceneQueueData(true));
        }
        #endregion
    }
}
