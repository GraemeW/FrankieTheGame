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
        bool pauseCombat = false;
        BattleState state = default;
        BattleOutcome outcome = default;

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

        public enum BattleOutcome
        {
            Undetermined,
            Won,
            Lost,
            Ran,
            Bargained
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
            if (battleStateChanged != null)
            {
                battleStateChanged.Invoke(state);
            }
        }

        public void SetBattleOutcome(BattleOutcome outcome)
        {
            this.outcome = outcome;
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
            state = BattleState.Intro;
            if (battleStateChanged != null)
            {
                battleStateChanged.Invoke(state);
            }
        }

        public IEnumerable GetEnemies()
        {
            return activeEnemies;
        }
    }
}