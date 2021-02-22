using Frankie.SceneManagement;
using Frankie.Stats;
using Frankie.Control;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Frankie.Combat.Skill;

namespace Frankie.Combat
{
    public class BattleController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] float battleQueueDelay = 1.0f;

        [Header("Interaction")]
        [SerializeField] string interactExecuteButton = "Fire1";
        [SerializeField] KeyCode interactExecuteKey = KeyCode.E;
        [SerializeField] string interactCancelButton = "Cancel";
        [SerializeField] KeyCode interactUp = KeyCode.W;
        [SerializeField] KeyCode interactLeft = KeyCode.A;
        [SerializeField] KeyCode interactRight = KeyCode.D;
        [SerializeField] KeyCode interactDown = KeyCode.S;
        [SerializeField] KeyCode interactPartyMember1 = KeyCode.Alpha1;
        [SerializeField] KeyCode interactPartyMember2 = KeyCode.Alpha2;
        [SerializeField] KeyCode interactPartyMember3 = KeyCode.Alpha3;
        [SerializeField] KeyCode interactPartyMember4 = KeyCode.Alpha4;

        // State
        BattleState state = default;
        BattleOutcome outcome = default;
        bool outroCleanupCalled = false;

        List<CombatParticipant> activeCharacters = new List<CombatParticipant>();
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();
        CombatParticipant selectedCharacter = null;
        Skill selectedSkill = null;
        bool skillArmed = false;
        CombatParticipant selectedEnemy = null;

        Queue<BattleSequence> battleSequenceQueue = new Queue<BattleSequence>();
        bool haltBattleQueue = false;

        // Cached References
        Party party = null;

        // Events
        public event Action<BattleState> battleStateChanged;
        public event Action<CombatParticipantType, CombatParticipant> selectedCombatParticipantChanged;
        public event Action<PlayerInputType> battleInput;
        public event Action<PlayerInputType> globalInput;
        public event Action<BattleSequence> battleSequenceProcessed;

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
                enemy.SetCombatActive(true);
                enemy.stateAltered += CheckForBattleEnd;
                activeEnemies.Add(enemy);
            }
            FindObjectOfType<Fader>().battleUIStateChanged += InitiateBattle;
        }

        public void SetBattleState(BattleState state)
        {
            this.state = state;

            if (state == BattleState.Combat)
            {
                ToggleCombatParticipants(true);
            }
            else
            {
                ToggleCombatParticipants(false);
            }

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
            if (character == null) { SetActiveSkill(null); }
            if (character != null) { if (character.IsDead() || character.IsInCooldown()) { return false; } }

            selectedCharacter = character;
            if (selectedCombatParticipantChanged != null)
            {
                selectedCombatParticipantChanged.Invoke(CombatParticipantType.Character, selectedCharacter);
            }
            return true;
        }

        public CombatParticipant GetSelectedCharacter()
        {
            return selectedCharacter;
        }

        public bool SetSelectedEnemy(CombatParticipant enemy)
        {
            if (enemy != null) { if (enemy.IsDead()) { return false; } }

            selectedEnemy = enemy;
            if (selectedCombatParticipantChanged != null)
            {
                selectedCombatParticipantChanged.Invoke(CombatParticipantType.Enemy, selectedEnemy);
            }
            return true;
        }

        public CombatParticipant GetSelectedEnemy()
        {
            return selectedEnemy;
        }

        public void SetActiveSkill(Skill skill)
        {
            if (skill == null)
            {
                SetSkillArmed(false);
                if (selectedCharacter != null) { selectedCharacter.GetComponent<SkillHandler>().ResetCurrentBranch(); }
            }

            selectedSkill = skill;
        }

        public Skill GetActiveSkill()
        {
            return selectedSkill;
        }

        public List<CombatParticipant> GetCharacters()
        {
            return activeCharacters;
        }

        public List<CombatParticipant> GetEnemies()
        {
            return activeEnemies;
        }

        public void AddToBattleQueue(CombatParticipant sender, CombatParticipant recipient, Skill skill)
        {
            sender.SetCooldown(Mathf.Infinity); // Character actions locked until cooldown set by BattleController
            BattleSequence battleSequence = new BattleSequence
            {
                sender = sender,
                recipient = recipient,
                skill = skill
            };
            battleSequenceQueue.Enqueue(battleSequence);
            if (activeCharacters.Contains(sender)) { SetSelectedCharacter(null); SetSelectedEnemy(null); } // Clear out selection on player execution
        }

        // Private Functions

        private void Awake()
        {
            party = GameObject.FindGameObjectWithTag("Player").GetComponent<Party>();
        }

        private void Update()
        {
            // TODO:  Update input system to NEW input system
            PlayerInputType playerInputType = GetPlayerInput();
            if (state == BattleState.Combat)
            {
                if (!haltBattleQueue) // BattleQueue takes priority, avoid user interaction stalling action queue
                {
                    StartCoroutine(ProcessNextBattleSequence());
                }
                if (InteractWithInterrupts(playerInputType)) { return; }
                if (InteractWithCharacterSelect()) { return; }
                if (InteractWithSkillSelect(playerInputType)) { return; }
                if (InteractWithSkillExecute(playerInputType)) { return; }
            }
            else if (state == BattleState.Outro)
            {
                if (!outroCleanupCalled)
                {
                    outroCleanupCalled = true;
                    // TODO:  Handle experience awards + levels ~ set state level up and handled full outro + level by canvas
                    CleanUpBattleBits();
                }
            }
            else if (state == BattleState.Complete && outroCleanupCalled) { outroCleanupCalled = false; }

            if (InteractWithGlobals(playerInputType)) { return; }  // Final call to globals, avoid short circuit
        }

        private void LateUpdate()
        {
            if (state == BattleState.Combat)
            {
                AutoSelectCharacter();
            }
        }

        private void InitiateBattle(bool isBattleCanvasEnabled)
        {
            if (isBattleCanvasEnabled)
            {
                FindObjectOfType<Fader>().battleUIStateChanged -= InitiateBattle;
                SetBattleState(BattleState.Intro);
            }
        }

        private void AutoSelectCharacter()
        {
            if (activeCharacters == null || selectedCharacter != null) { return; }
            CombatParticipant firstFreeCharacter = activeCharacters.Where(x => !x.IsInCooldown()).FirstOrDefault();
            if (firstFreeCharacter != null) { SetSelectedCharacter(firstFreeCharacter); }
        }

        private void ToggleCombatParticipants(bool enable)
        {
            foreach (CombatParticipant character in activeCharacters)
            {
                character.SetCombatActive(enable);
            }
            foreach (CombatParticipant enemy in activeEnemies)
            {
                enemy.SetCombatActive(enable);
            }
        }

        private bool InteractWithInterrupts(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.Cancel)
            {
                // Move to Combat Options if nothing selected in skill selector
                if (selectedSkill == null || selectedCharacter == null) 
                {
                    SetSelectedEnemy(null);
                    SetSelectedCharacter(null);
                    SetBattleState(BattleState.PreCombat); return true; 
                }

                // Otherwise step out of selections
                if (selectedSkill != null || selectedCharacter != null)
                {
                    SetSelectedEnemy(null);
                    SetSelectedCharacter(null);
                }
                return true;
            }
            return false;
        }

        private bool InteractWithCharacterSelect()
        {
            if (GetBattleState() !=  BattleState.Combat) { return false; }

            int numberOfPartyMembers = party.GetParty().Count;
            bool validInput = false;
            CombatParticipant candidateCharacter = null;

            if (Input.GetKeyDown(interactPartyMember1))
            {
                validInput = true;
                candidateCharacter = party.GetParty()[0];
            }
            else if (Input.GetKeyDown(interactPartyMember2) && numberOfPartyMembers >= 2)
            {
                validInput = true;
                candidateCharacter = party.GetParty()[1];
            }
            else if (Input.GetKeyDown(interactPartyMember3) && numberOfPartyMembers >= 3)
            {
                validInput = true;
                candidateCharacter = party.GetParty()[2];
            }
            else if (Input.GetKeyDown(interactPartyMember4) && numberOfPartyMembers >= 4)
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

        private bool InteractWithSkillSelect(PlayerInputType playerInputType)
        {
            if (GetBattleState() != BattleState.Combat) { return false; }
            if (skillArmed) { return false; }

            if (selectedCharacter != null && !selectedCharacter.IsDead())
            {
                if (playerInputType == PlayerInputType.Execute && selectedSkill != null)
                {
                    SetSkillArmed(true);
                    return true;
                }

                if (playerInputType != PlayerInputType.DefaultNone)
                {
                    if (battleInput != null)
                    {
                        battleInput.Invoke(playerInputType);
                    }
                    return true;
                }
            }
            return false;
        }

        private bool InteractWithSkillExecute(PlayerInputType playerInputType)
        {
            if (GetBattleState() != BattleState.Combat) { return false; }
            if (!skillArmed || selectedCharacter == null || selectedSkill == null) { return false; }

            if (playerInputType != PlayerInputType.DefaultNone)
            {
                if (playerInputType == PlayerInputType.Execute)
                {
                    if (selectedEnemy == null) { return false; }
                    AddToBattleQueue(GetSelectedCharacter(), GetSelectedEnemy(), GetActiveSkill());
                }
                else
                {
                    if (playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
                    {
                        SetSelectedEnemy(GetNextLivingEnemy(GetSelectedEnemy(), true));
                    }
                    else if (playerInputType == PlayerInputType.NavigateLeft || playerInputType == PlayerInputType.NavigateUp)
                    {
                        SetSelectedEnemy(GetNextLivingEnemy(GetSelectedEnemy(), false));
                    }
                }

                if (battleInput != null)
                {
                    battleInput.Invoke(playerInputType);
                }
                return true;
            }
            return false;
        }

        private bool InteractWithGlobals(PlayerInputType playerInputType)
        {
            if (Input.GetButtonDown(interactExecuteButton) || Input.GetKeyDown(interactExecuteKey)) // Override to allow both mouse click and keyboard input
            {
                if (globalInput != null)
                {
                    globalInput.Invoke(PlayerInputType.Execute);
                    return true;
                }
            }
            else
            {
                if (globalInput != null)
                {
                    globalInput.Invoke(playerInputType);
                    return true;
                }
            }
            return false;
        }

        public PlayerInputType GetPlayerInput()
        {
            PlayerInputType input = PlayerInputType.DefaultNone;
            if (Input.GetKeyDown(interactUp))
            {
                input = PlayerInputType.NavigateUp;
            }
            else if (Input.GetKeyDown(interactLeft))
            {
                input = PlayerInputType.NavigateLeft;
            }
            else if (Input.GetKeyDown(interactRight))
            {
                input = PlayerInputType.NavigateRight;
            }
            else if (Input.GetKeyDown(interactDown))
            {
                input = PlayerInputType.NavigateDown;
            }
            else if (Input.GetKeyDown(interactExecuteKey))
            {
                input = PlayerInputType.Execute;
            }
            else if (Input.GetButtonDown(interactCancelButton))
            {
                input = PlayerInputType.Cancel;
            }
            return input;
        }

        public void SetSkillArmed(bool enable)
        {
            if (!enable) { selectedEnemy = null; skillArmed = false; return; }

            if (SelectFirstLivingEnemy())
            {
                skillArmed = enable;
            }
        }

        public bool IsSkillArmed()
        {
            return skillArmed;
        }

        private bool SelectFirstLivingEnemy()
        {
            CombatParticipant[] livingEnemies = activeEnemies.Where(x => !x.IsDead()).ToArray();
            if (livingEnemies.Length > 0)
            {
                return SetSelectedEnemy(livingEnemies[0]);
            }
            return false;
        }

        private CombatParticipant GetNextLivingEnemy(CombatParticipant currentEnemy, bool traverseForward)
        {
            if (selectedEnemy == null) { SelectFirstLivingEnemy(); return selectedEnemy; }

            int currentIndex = activeEnemies.IndexOf(currentEnemy);
            if (traverseForward)
            {
                if (currentIndex + 1 >= activeEnemies.Count) { currentIndex = 0; }
                else { currentIndex++; }
            }
            else
            {
                if (currentIndex <= 0) { currentIndex = activeEnemies.Count - 1; }
                else { currentIndex--; }
            }

            if (activeEnemies[currentIndex].IsDead()) { return GetNextLivingEnemy(activeEnemies[currentIndex], traverseForward); }
            return activeEnemies[currentIndex];
        }

        IEnumerator ProcessNextBattleSequence()
        {
            if (battleSequenceQueue.Count == 0) { yield break; }
            BattleSequence battleSequence = battleSequenceQueue.Dequeue();
            if (battleSequence.sender.IsDead() || battleSequence.recipient.IsDead()) { yield break; }
            battleSequenceProcessed.Invoke(battleSequence);

            haltBattleQueue = true;
            for (int i = 0; i < battleSequence.skill.numberOfHits; i++)
            {
                battleSequence.sender.SetCooldown(battleSequence.sender.GetCooldownForSkill(battleSequence.skill));
                battleSequence.recipient.AdjustHP(battleSequence.sender.GetHPValueForSkill(battleSequence.skill));
                battleSequence.recipient.AdjustAP(battleSequence.sender.GetAPValueForSkill(battleSequence.skill));
                foreach (StatusProbabilityPair activeStatusEffect in battleSequence.skill.statusEffects)
                {
                    battleSequence.recipient.ApplyStatusEffect(activeStatusEffect.statusEffect);
                }
                yield return new WaitForSeconds(battleQueueDelay);
            }
            haltBattleQueue = false;
        }

        private void CheckForBattleEnd(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType == StateAlteredType.Dead)
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
                character.SetCombatActive(false);
                character.stateAltered -= CheckForBattleEnd;
            }
            foreach (CombatParticipant enemy in activeEnemies)
            {
                enemy.SetCombatActive(false);
                enemy.stateAltered -= CheckForBattleEnd;
            }
            activeCharacters.Clear();
            activeEnemies.Clear();
            SetSelectedCharacter(null);
        }
    }
}