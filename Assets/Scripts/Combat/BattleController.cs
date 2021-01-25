using Frankie.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class BattleController : MonoBehaviour
    {
        // State
        bool pauseCombat = false;
        BattleState state = default;
        BattleOutcome outcome = default;
        List<CombatParticipant> activePlayerCharacters = new List<CombatParticipant>();
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();
        CombatParticipant selectedPlayerCharacter = null;

        // Events
        public event Action<BattleState> battleStateChanged;
        public event Action<CombatParticipant> selectedPlayerCharacterChanged;
        public event Action<string> battleInput;

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
            // TODO:  Implement concept of party and multiple player members

            activePlayerCharacters.Add(GetComponent<CombatParticipant>()); // HACK:  combat participant living on player, where battle handler lives -- see TODO
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

        public BattleState GetBattleState()
        {
            return state;
        }

        public void SetBattleOutcome(BattleOutcome outcome)
        {
            this.outcome = outcome;
        }

        public void SetActivePlayerCharacter(CombatParticipant playerCombatParticipant)
        {
            selectedPlayerCharacter = playerCombatParticipant;
        }

        public CombatParticipant GetActivePlayerCharacter()
        {
            return selectedPlayerCharacter;
        }

        public IEnumerable GetEnemies()
        {
            return activeEnemies;
        }

        // Private Functions
        private void Update()
        {
            if (state == BattleState.Combat)
            {
                InteractWithInterrupts();
                InteractWithSkill();
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

        private void InteractWithSkill()
        {
            // TODO:  Update input system to NEW input system
            if (selectedPlayerCharacter != null && !selectedPlayerCharacter.IsDead())
            {
                string input = null;
                if (Input.GetKeyDown(KeyCode.W))
                {
                    input = "up";
                }
                else if (Input.GetKeyDown(KeyCode.A))
                {
                    input = "left";
                }
                else if (Input.GetKeyDown(KeyCode.D))
                {
                    input = "right";
                }
                else if (Input.GetKeyDown(KeyCode.S))
                {
                    input = "down";
                }
                if (!string.IsNullOrWhiteSpace(input))
                {
                    if (battleInput != null)
                    {
                        battleInput.Invoke(input);
                    }
                }
            }
        }

        private void InteractWithInterrupts()
        {
            if (Input.GetButtonDown("Cancel"))
            {
                SetBattleState(BattleState.PreCombat);
            }
        }
    }
}