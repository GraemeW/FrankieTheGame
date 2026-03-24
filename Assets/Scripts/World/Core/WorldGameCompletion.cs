using UnityEngine;
using Frankie.Control;
using Frankie.ZoneManagement;

namespace Frankie.World
{
    public class WorldGameCompletion : MonoBehaviour
    {
        #region PublicMethods
        public void TriggerGameOver(PlayerStateMachine playerStateMachine)
        {
            playerStateMachine.EnterCutscene();
            SceneLoader.QueueScene(SceneQueueType.GameOver, new SceneQueueData(true));
        }

        public void TriggerGameWin(PlayerStateMachine playerStateMachine)
        {
            playerStateMachine.EnterCutscene();
            SceneLoader.QueueScene(SceneQueueType.GameWin, new SceneQueueData(true));
        }
        #endregion
    }
}
