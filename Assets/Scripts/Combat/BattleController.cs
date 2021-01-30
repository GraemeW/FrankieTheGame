using Frankie.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frankie.Combat.Skill;

namespace Frankie.Combat
{
    public class BattleController : MonoBehaviour
    {
        // Tunables
        [SerializeField] float battleQueueDelay = 0.5f;

        // State
        bool pauseCombat = false;
        BattleState state = default;
        BattleOutcome outcome = default;

        List<CombatParticipant> activePlayerCharacters = new List<CombatParticipant>();
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();
        CombatParticipant selectedPlayerCharacter = null;
        Skill selectedSkill = null;

        Queue<BattleSequence> battleSequenceQueue = new Queue<BattleSequence>();
        bool haltBattleQueue = false;

        // Cached References
        GameObject player = null;

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

        public struct BattleSequence
        {
            public CombatParticipant sender;
            public CombatParticipant recipient;
            public Skill skill;
        }

        // Public Functions
        public void Setup(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            // TODO:  Implement different battle transitions
            // TODO:  Implement concept of party and multiple player members
            activePlayerCharacters.Add(player.GetComponent<CombatParticipant>());
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

        public void SetActiveSkill(Skill skill)
        {
            selectedSkill = skill;
        }

        public Skill GetActiveSkill()
        {
            return selectedSkill;
        }

        public IEnumerable GetEnemies()
        {
            return activeEnemies;
        }

        public void AddToBattleQueue(CombatParticipant sender, CombatParticipant recipient, Skill skill)
        {
            BattleSequence battleSequence = new BattleSequence
            {
                sender = sender,
                recipient = recipient,
                skill = skill
            };
            battleSequenceQueue.Enqueue(battleSequence);
        }

        // Private Functions

        private void Awake()
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        private void Update()
        {
            if (state == BattleState.Combat)
            {
                InteractWithInterrupts();
                InteractWithSkill();
                if (!haltBattleQueue)
                {
                    StartCoroutine(HaltBattleQueue(battleQueueDelay));
                    ProcessNextBattleSequence();
                }
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

        IEnumerator HaltBattleQueue(float seconds)
        {
            haltBattleQueue = true;
            yield return new WaitForSeconds(seconds);
            haltBattleQueue = false;
        }

        private void ProcessNextBattleSequence()
        {
            if (battleSequenceQueue.Count == 0) { return; }

            BattleSequence battleSequence = battleSequenceQueue.Dequeue();
            if (battleSequence.sender.IsDead() || battleSequence.recipient.IsDead()) { return; }

            battleSequence.recipient.AdjustHP(battleSequence.skill.hpValue);
            battleSequence.recipient.AdjustAP(battleSequence.skill.apValue);
            foreach (StatusProbabilityPair activeStatusEffect in battleSequence.skill.statusEffects)
            {
                // TODO:  add logic for applying statuses, needs to be cleaner
                battleSequence.recipient.ApplyStatusEffect(activeStatusEffect.statusEffect);
            }
        }
    }
}