using Frankie.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class BattleHandler : MonoBehaviour
    {
        // State
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();

        // Events
        public event Action enemiesUpdated;

        public void Setup(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            activeEnemies = enemies;
            FindObjectOfType<Fader>().battleCanvasEnabled += LoadEnemies;
        }

        private void LoadEnemies()
        {
            if (enemiesUpdated != null)
            {
                enemiesUpdated.Invoke();
            }
        }

        public IEnumerable GetEnemies()
        {
            return activeEnemies;
        }
    }
}