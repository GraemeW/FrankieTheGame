using UnityEngine;
using Frankie.Core;

namespace Frankie.Control.Specialization
{
    public class WorldGameCompletion : MonoBehaviour
    {
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

    }
}
