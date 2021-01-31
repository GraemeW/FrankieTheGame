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
        BattleState state = default;
        BattleOutcome outcome = default;
        bool outroCleanupCalled = false;

        List<CombatParticipant> activeCharacters = new List<CombatParticipant>();
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();
        CombatParticipant selectedCharacter = null;
        public Skill selectedSkill = null;

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

        public bool SetSelectedCharacter(CombatParticipant character)
        {
            if (character != null) { if (character.IsDead() || character.IsInCooldown()) { return false; } }
            if (character == null) { SetActiveSkill(null); }

            selectedCharacter = character;
            if (selectedCharacterChanged != null)
            {
                selectedCharacterChanged.Invoke(selectedCharacter);
            }
            return true;
        }

        public CombatParticipant GetSelectedCharacter()
        {
            return selectedCharacter;
        }

        public void SetActiveSkill(Skill skill)
        {
            if (selectedCharacter != null && skill == null) { selectedCharacter.GetComponent<SkillHandler>().ResetCurrentBranch(); }
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
            SetSelectedCharacter(null);
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
                if (!haltBattleQueue) // BattleQueue takes priority, avoid user interaction stalling action queue
                {
                    StartCoroutine(HaltBattleQueue(battleQueueDelay));
                    ProcessNextBattleSequence();
                }

                // TODO:  Update input system to NEW input system
                if (InteractWithInterrupts()) { return; }
                if (InteractWithCharacterSelect()) { return; }
                if (InteractWithSkillSelect()) { return; }
                // TODO:  add InteractWithSkillExecute
            }
            else if (state == BattleState.Outro)
            {
                if (outroCleanupCalled) { return; }

                outroCleanupCalled = true;
                // TODO:  Handle experience awards + levels ~ set state level up and handled full outro + level by canvas
                CleanUpBattleBits();
            }
            else if (state == BattleState.Complete && outroCleanupCalled) { outroCleanupCalled = false; }
        }

        private void LateUpdate()
        {
            if (state == BattleState.Combat)
            {
                AutoSelectCharacter();
            }
        }

        private void InitiateBattle()
        {
            FindObjectOfType<Fader>().battleCanvasEnabled -= InitiateBattle;
            SetBattleState(BattleState.Intro);
        }

        private void AutoSelectCharacter()
        {
            if (activeCharacters == null || selectedCharacter != null) { return; }
            CombatParticipant firstFreeCharacter = activeCharacters.Where(x => !x.IsInCooldown()).FirstOrDefault();
            if (firstFreeCharacter != null) { SetSelectedCharacter(firstFreeCharacter); }
        }

        private bool InteractWithInterrupts()
        {
            if (Input.GetButtonDown("Cancel"))
            {
                // Move to Combat Options if nothing selected in skill selector
                if (selectedSkill == null || selectedCharacter == null) 
                {
                    SetSelectedCharacter(null);
                    SetBattleState(BattleState.PreCombat); return true; 
                }

                // Otherwise step out of selections
                if (selectedSkill != null || selectedCharacter != null)
                {
                    SetSelectedCharacter(null);
                }
                return true;
            }
            return false;
        }

        private bool InteractWithCharacterSelect()
        {
            int numberOfPartyMembers = party.GetParty().Count;
            bool validInput = false;
            CombatParticipant candidateCharacter = null;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                validInput = true;
                candidateCharacter = party.GetParty()[0];
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) && numberOfPartyMembers >= 2)
            {
                validInput = true;
                candidateCharacter = party.GetParty()[1];
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) && numberOfPartyMembers >= 3)
            {
                validInput = true;
                candidateCharacter = party.GetParty()[2];
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) && numberOfPartyMembers >= 4)
            {
                validInput = true;
                candidateCharacter = party.GetParty()[3];
            }
            if (validInput)
            {
                validInput = SetSelectedCharacter(candidateCharacter);
            }
            return validInput;
        }

        private bool InteractWithSkillSelect()
        {
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
                    return true;
                }
            }
            return false;
        }

        private bool InteractWithSkillExecute()
        {
            return false;
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

            battleSequence.sender.SetCooldown(battleSequence.skill.cooldown);
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
                    SetBattleState(BattleState.Outro);

                }
                else if (activeEnemies.All(x => x.IsDead() == true))
                {
                    SetBattleOutcome(BattleOutcome.Won);
                    SetBattleState(BattleState.Outro);
                }
            }
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
            SetSelectedCharacter(null);
        }
    }
}