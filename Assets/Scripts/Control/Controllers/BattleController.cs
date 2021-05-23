using Frankie.Core;
using Frankie.Stats;
using Frankie.Control;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.ZoneManagement;

namespace Frankie.Combat
{
    public class BattleController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] float battleQueueDelay = 1.0f;
        [SerializeField] float runFailureCooldown = 3.0f;
        [SerializeField] float itemCooldown = 3.0f;

        // State
        BattleState state = default;
        BattleOutcome outcome = default;
        bool outroCleanupCalled = false;
        bool handleLevelUp = false;
        float battleExperienceReward = 0f;

        List<CombatParticipant> activeCharacters = new List<CombatParticipant>();
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();
        [SerializeField] CombatParticipant selectedCharacter = null; // Serialized for Debug
        [SerializeField] BattleAction selectedBattleAction = BattleAction.None;
        [SerializeField] bool battleActionArmed = false;
        [SerializeField] CombatParticipant selectedEnemy = null;

        Queue<BattleSequence> battleSequenceQueue = new Queue<BattleSequence>();
        bool haltBattleQueue = false;

        // Cached References
        PlayerInput playerInput = null;
        Party party = null;

        // Events
        public event Action<BattleState> battleStateChanged;
        public event Action<CombatParticipantType, CombatParticipant> selectedCombatParticipantChanged;
        public event Action<PlayerInputType> battleInput;
        public event Action<PlayerInputType> globalInput;
        public event Action<BattleSequence> battleSequenceProcessed;

        // Interaction
        private void Awake()
        {
            party = GameObject.FindGameObjectWithTag("Player").GetComponent<Party>();

            playerInput = new PlayerInput();
            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Execute.performed += context => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += context => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Menu.Skip.performed += context => HandleUserInput(PlayerInputType.Skip);
            playerInput.Menu.Select1.performed += context => InteractWithCharacterSelect(0);
            playerInput.Menu.Select2.performed += context => InteractWithCharacterSelect(1);
            playerInput.Menu.Select3.performed += context => InteractWithCharacterSelect(2);
            playerInput.Menu.Select4.performed += context => InteractWithCharacterSelect(3);
        }

        private void OnEnable()
        {
            playerInput.Menu.Enable();
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
        }

        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            PlayerInputType playerInputType = this.NavigationVectorToInputType(directionalInput);
            HandleUserInput(playerInputType);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            if (state == BattleState.Combat)
            {
                if (InteractWithInterrupts(playerInputType)) { return; }
                if (InteractWithSkillSelect(playerInputType)) { return; }
                if (InteractWithBattleActionExecute(playerInputType)) { return; }
            }

            // Final call to globals, avoid short circuit
            if (InteractWithGlobals(playerInputType)) { return; }
        }

        private void Update()
        {
            if (state == BattleState.Combat)
            {
                if (!haltBattleQueue) // BattleQueue takes priority, avoid user interaction stalling action queue
                {
                    StartCoroutine(ProcessNextBattleSequence());
                }

            }
            else if (state == BattleState.Outro)
            {
                if (!handleLevelUp && !outroCleanupCalled)
                {
                    outroCleanupCalled = true;
                    CleanUpBattleBits();
                }
            }
            else if (state == BattleState.Complete && outroCleanupCalled) { outroCleanupCalled = false; }
        }

        private void LateUpdate()
        {
            if (state == BattleState.Combat &&
                selectedBattleAction.battleActionType != BattleActionType.ActionItem) // Item use handling;  suppresses character selection -> Skill Selection visible
            {
                AutoSelectCharacter();
            }
        }

        private bool InteractWithInterrupts(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.Cancel)
            {
                // Move to Combat Options if nothing selected in skill selector
                if (selectedBattleAction.battleActionType == BattleActionType.None || selectedCharacter == null)
                {
                    SetSelectedEnemy(null);
                    SetSelectedCharacter(null);
                    SetBattleState(BattleState.PreCombat); return true;
                }

                // Otherwise step out of selections
                if (selectedBattleAction.battleActionType != BattleActionType.None || selectedCharacter != null)
                {
                    SetSelectedEnemy(null);
                    SetActiveBattleAction(BattleAction.None);
                    SetSelectedCharacter(null);
                    SetBattleState(BattleState.Combat);
                }
                return true;
            }
            return false;
        }

        private void InteractWithCharacterSelect(int partyMemberSelect)
        {
            if (GetBattleState() != BattleState.Combat) { return; }
            if (partyMemberSelect >= party.GetParty().Count) { return; } // >= since indexing off by 1 from count

            SetSelectedCharacter(party.GetParty()[partyMemberSelect]);
        }

        private bool InteractWithSkillSelect(PlayerInputType playerInputType)
        {
            if (GetBattleState() != BattleState.Combat) { return false; }
            if (IsBattleActionArmed()) { return false; }

            if (selectedCharacter != null && !selectedCharacter.IsDead())
            {
                if (playerInputType == PlayerInputType.Execute && selectedBattleAction.battleActionType != BattleActionType.None)
                {
                    SetBattleActionArmed(true);
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

        private bool InteractWithBattleActionExecute(PlayerInputType playerInputType)
        {

            if (GetBattleState() != BattleState.Combat) { return false; }
            if (!IsBattleActionArmed() || selectedCharacter == null || selectedBattleAction.battleActionType == BattleActionType.None) { return false; }

            if (playerInputType != PlayerInputType.DefaultNone)
            {
                if (playerInputType == PlayerInputType.Execute)
                {
                    if (selectedEnemy == null) { return false; }
                    AddToBattleQueue(GetSelectedCharacter(), GetSelectedEnemy(), selectedBattleAction);
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
            if (playerInputType != PlayerInputType.DefaultNone && globalInput != null)
            {
                globalInput.Invoke(playerInputType);
                return true;
            }
            return false;
        }

        // External Setup
        public void Setup(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            foreach (CombatParticipant character in party.GetParty())
            {
                if (transitionType == TransitionType.BattleGood)
                {
                    character.SetCooldown(0f);
                }
                else
                {
                    character.SetCooldown(character.GetBattleStartCooldown());
                }
                character.stateAltered += CheckForBattleEnd;
                activeCharacters.Add(character);
            }
            foreach (CombatParticipant enemy in enemies)
            {
                if (transitionType == TransitionType.BattleBad)
                {
                    enemy.SetCooldown(0f);
                }
                else
                {
                    enemy.SetCooldown(enemy.GetBattleStartCooldown());
                }
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

        public void SetHandleLevelUp(bool enable)
        {
            handleLevelUp = enable;
        }

        public bool SetSelectedCharacter(CombatParticipant character)
        {
            if (character == null) { SetActiveBattleAction(BattleAction.None); }
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

        public void SetActiveBattleAction(BattleAction battleAction)
        {
            if (battleAction.battleActionType == BattleActionType.None)
            {
                SetBattleActionArmed(false);
                if (selectedCharacter != null) { selectedCharacter.GetComponent<SkillHandler>().ResetCurrentBranch(); }
                selectedBattleAction = BattleAction.None;
            }
            else
            {
                selectedBattleAction = battleAction;
            }
        }

        public BattleAction GetActiveBattleAction()
        {
            return selectedBattleAction;
        }

        public List<CombatParticipant> GetCharacters()
        {
            return activeCharacters;
        }

        public List<CombatParticipant> GetEnemies()
        {
            return activeEnemies;
        }

        public bool AddToBattleQueue(CombatParticipant recipient)
        {
            // Called via SkillSelection UI Buttons
            // Using selected character and battle action
            if (GetSelectedCharacter() == null || selectedBattleAction.battleActionType == BattleActionType.None) { return false; }
            
            AddToBattleQueue(GetSelectedCharacter(), recipient, selectedBattleAction);
            return true;
        }

        public void AddToBattleQueue(CombatParticipant sender, CombatParticipant recipient, BattleAction battleAction)
        {
            BattleSequence battleSequence = new BattleSequence
            {
                battleAction = battleAction,
                sender = sender,
                recipient = recipient,
            };
            AddToBattleQueue(sender, battleSequence);
        }

        private void AddToBattleQueue(CombatParticipant sender, BattleSequence battleSequence)
        {
            sender.SetCooldown(Mathf.Infinity); // Character actions locked until cooldown set by BattleController
            battleSequenceQueue.Enqueue(battleSequence);

            if (activeCharacters.Contains(sender)) { SetSelectedEnemy(null); SetActiveBattleAction(BattleAction.None); SetSelectedCharacter(null);  } // Clear out selection on player execution
        }

        // Battle Handling
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

        public void SetBattleActionArmed(bool enable)
        {
            if (!enable) { selectedEnemy = null; battleActionArmed = false; return; }

            if (SelectFirstLivingEnemy())
            {
                battleActionArmed = true;
            }
        }

        public bool IsBattleActionArmed()
        {
            return battleActionArmed;
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
            if (battleSequence.battleAction.battleActionType == BattleActionType.Skill)
            {
                yield return SkillHandler.Use(battleSequence.battleAction.skill, battleSequence.sender, battleSequence.recipient, battleQueueDelay);
            }
            else if (battleSequence.battleAction.battleActionType == BattleActionType.ActionItem)
            {
                battleSequence.sender.SetCooldown(itemCooldown);
                battleSequence.sender.GetKnapsack().UseItem(battleSequence.battleAction.actionItem, battleSequence.recipient);
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
                    AwardExperience();
                    SetBattleState(BattleState.Outro);
                }
            }
        }

        private void AwardExperience()
        {
            foreach (CombatParticipant character in GetCharacters())
            {
                Experience experience = character.GetExperience();
                if (experience == null) { return; } // Handling for characters who do not level
                float scaledExperienceReward = 0f;

                foreach (CombatParticipant enemy in GetEnemies())
                {
                    float rawExperienceReward = enemy.GetExperienceReward();
                    battleExperienceReward += rawExperienceReward;

                    int levelDelta = character.GetLevel() - enemy.GetLevel();
                    scaledExperienceReward += Experience.GetScaledExperience(rawExperienceReward, levelDelta, experience.GetExperienceScaling());
                }

                if (experience.GainExperienceToLevel(scaledExperienceReward))
                {
                    handleLevelUp = true;
                }
            }
        }

        public float GetBattleExperienceReward()
        {
            return battleExperienceReward;
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

        public bool AttemptToRun()
        {
            float partySpeed = 0f;
            float enemySpeed = 0f;

            foreach (CombatParticipant character in activeCharacters)
            {
                partySpeed += character.GetBaseStats().GetBaseStat(Stat.Nimble);
            }

            foreach (CombatParticipant enemy in activeEnemies)
            {
                enemySpeed += enemy.GetBaseStats().GetBaseStat(Stat.Nimble);
            }

            if (partySpeed > enemySpeed)
            {
                SetBattleOutcome(BattleOutcome.Ran);
                SetBattleState(BattleState.Outro);
                return true;
            }
            else
            {
                foreach (CombatParticipant character in activeCharacters)
                {
                    if (character.GetCooldown() < runFailureCooldown)
                    {
                        character.SetCooldown(runFailureCooldown);
                    }
                }
                return false;
            }
        }

        public PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector)
        {
            // Not evaluated -> IStandardPlayerInputCallerExtension
            return PlayerInputType.DefaultNone;
        }
    }
}