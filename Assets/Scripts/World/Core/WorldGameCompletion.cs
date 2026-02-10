using UnityEngine;
using Frankie.Control;
using Frankie.Core;

namespace Frankie.World
{
    public class WorldGameCompletion : MonoBehaviour
    {
        #region PublicMethods
        public void TriggerGameOver(PlayerStateMachine playerStateMachine)
        {
            playerStateMachine.EnterCutscene();
            SavingWrapper.LoadGameOverScene();
        }

        public void TriggerGameWin(PlayerStateMachine playerStateMachine)
        {
            playerStateMachine.EnterCutscene();
            SavingWrapper.LoadGameWinScreen();
        }
        #endregion
    }
}
