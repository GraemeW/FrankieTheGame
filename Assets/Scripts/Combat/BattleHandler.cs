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
        BattleState state = default;

        // Events
        public event Action<BattleState> battleStateChanged;

        // Data structures
        public enum BattleState
        {
            Intro,
            PreCombat,
            Combat,
            Outro
        }

        // Public Functions
        public void Setup(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            // TODO:  Implement different battle transitions
            activeEnemies = enemies;
            FindObjectOfType<Fader>().battleCanvasEnabled += InitiateBattle;
        }

        public void SetBattleState(BattleState state)
        {
            this.state = state;
        }

        // Private Functions
        private void Update()
        {
            if (state == BattleState.Combat)
            {
                // Main battle loop
            }
        }

        private void InitiateBattle()
        {
            if (battleStateChanged != null)
            {
                state = BattleState.Intro;
                battleStateChanged.Invoke(state);
            }
        }

        public IEnumerable GetEnemies()
        {
            return activeEnemies;
        }
    }
}