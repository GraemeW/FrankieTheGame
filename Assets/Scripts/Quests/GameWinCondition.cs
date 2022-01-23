using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;

namespace Frankie.Quests
{
    public class GameWinCondition : MonoBehaviour
    {
        public void TriggerGameWin()
        {
            SavingWrapper.LoadGameWinScreen();
        }
    }
}