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
        bool isBattleActive = false;

        // Events
        public event Action enemiesUpdated;

        // Public Functions
        public void Setup(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            activeEnemies = enemies;
            FindObjectOfType<Fader>().battleCanvasEnabled += LoadEnemies;
        }

        public void SetBattleActive(bool state)
        {
            isBattleActive = state;
        }

        // Private Functions
        private void Update()
        {
            if (isBattleActive)
            {
                // Main battle loop
            }
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