using Frankie.Core;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Frankie.Combat.CombatParticipant;
using static Frankie.Combat.Skill;

namespace Frankie.Combat
{
    public class BattleController : MonoBehaviour
    {
        // Tunables
        [SerializeField] float battleQueueDelay = 0.3f;

        // State
        bool pauseCombat = false;
        BattleState state = default;
        BattleOutcome outcome = default;

        List<CombatParticipant> activeCharacters = new List<CombatParticipant>();
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();
        CombatParticipant selectedCharacter = null;
        Skill selectedSkill = null;

        Queue<BattleSequence> battleSequenceQueue = new Queue<BattleSequence>();
        bool haltBattleQueue = false;

        // Cached References
        Party party = null;

        // Events
        public event Action<BattleState> battleStateChanged;
        public event Action<CombatParticipant> selectedCharacterChanged;
        public event Action<string> battleInput;

        // Data structures
        public enum BattleState
        {
            Intro,
            PreCombat,
            Combat,
            Outro,
            LevelUp,
            Complete
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
            // TODO:  Implement different battle transition type impact to combat (i.e. advance attack, late attack, same-same)
            foreach (CombatParticipant character in party.GetParty())
            {
                character.stateAltered += CheckForBattleEnd;
                activeCharacters.Add(character);
            }
            foreach (CombatParticipant enemy in enemies)
            {
                enemy.stateAltered += CheckForBattleEnd;
                activeEnemies.Add(enemy);
            }
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

        public BattleOutcome GetBattleOutcome()
        {
            return outcome;
        }

        public void setSelectedCharacter(CombatParticipant character)
        {
            selectedCharacter = character;
            if (selectedCharacterChanged != null)
            {
                selectedCharacterChanged.Invoke(selectedCharacter);
            }
        }

        public CombatParticipant GetSelectedCharacter()
        {
            return selectedCharacter;
        }

        public void SetActiveSkill(Skill skill)
        {
            selectedSkill = skill;
        }

        public Skill GetActiveSkill()
        {
            return selectedSkill;
        }

        public IEnumerable GetCharacters()
        {
            return activeCharacters;
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
            party = GameObject.FindGameObjectWithTag("Player").GetComponent<Party>();
        }

        private void Update()
        {
            if (state == BattleState.Combat)
            {
                InteractWithInterrupts();
                // TODO:  add InteractWithPlayerSelect
                InteractWithSkillSelect();
                // TODO:  add InteractWithSkillExecute
                if (!haltBattleQueue)
                {
                    StartCoroutine(HaltBattleQueue(battleQueueDelay));
                    ProcessNextBattleSequence();
                }
                // TODO:  add check victory/loss conditions
            }
        }

        private void InitiateBattle()
        {
            FindObjectOfType<Fader>().battleCanvasEnabled -= InitiateBattle;
            SetBattleState(BattleState.Intro);
        }

        private void InteractWithSkillSelect()
        {
            // TODO:  Update input system to NEW input system
            if (selectedCharacter != null && !selectedCharacter.IsDead())
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
                selectedCharacter = null;
                selectedSkill = null;
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

        private void CheckForBattleEnd(StateAlteredType stateAlteredType)
        {
            if (stateAlteredType == StateAlteredType.Dead)
            {
                if (activeCharacters.All(x => x.IsDead() == true))
                {
                    SetBattleOutcome(BattleOutcome.Lost);
                    CloseOutBattle();
                    
                }
                else if (activeEnemies.All(x => x.IsDead() == true))
                {
                    SetBattleOutcome(BattleOutcome.Won);
                    CloseOutBattle();
                }
            }
        }

        private void CloseOutBattle()
        {
            // Handle experience awards + levels ~ set state level up and handled full outro + level by canvas
            CleanUpBattleBits();
            SetBattleState(BattleState.Outro);
        }

        private void CleanUpBattleBits()
        {
            foreach (CombatParticipant character in activeCharacters)
            {
                character.stateAltered -= CheckForBattleEnd;
            }
            foreach (CombatParticipant enemy in activeEnemies)
            {
                enemy.stateAltered -= CheckForBattleEnd;
            }
            activeCharacters.Clear();
            activeEnemies.Clear();
            selectedCharacter = null;
            selectedSkill = null;
        }
    }
}