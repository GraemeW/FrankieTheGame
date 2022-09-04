using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;

namespace Frankie.Control.Specialization
{
    public class WorldGameCompletion : MonoBehaviour
    {
        public void TriggerGameOver(PlayerStateMachine playerStateMachine)
        {
            SavingWrapper.LoadGameOverScene();
        }

        public void TriggerGameWin(PlayerStateMachine playerStateMachine)
        {
            SavingWrapper.LoadGameWinScreen();
        }

    }
}
