using Frankie.Core;
using Frankie.Stats;
using Frankie.Control;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Frankie.Combat.Skill;
using Frankie.ZoneManagement;

namespace Frankie.Combat
{
    public class BattleController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] float battleQueueDelay = 1.0f;
        [SerializeField] float runFailureCooldown = 3.0f;

        // State
        BattleState state = default;
        BattleOutcome outcome = default;
        bool outroCleanupCalled = false;
        bool handleLevelUp = false;
        float battleExperienceReward = 0f;

        List<CombatParticipant> activeCharacters = new List<CombatParticipant>();
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();
        CombatParticipant selectedCharacter = null;
        Skill selectedSkill = null;
        bool skillArmed = false;
        CombatParticipant selectedEnemy = null;

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
                if (InteractWithSkillExecute(playerInputType)) { return; }
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
            if (state == BattleState.Combat)
            {
                AutoSelectCharacter();
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

        private void InteractWithCharacterSelect(int partyMemberSelect)
        {
            if (GetBattleState() != BattleState.Combat) { return; }
            if (partyMemberSelect >= party.GetParty().Count) { return; } // >= since indexing off by 1 from count

            SetSelectedCharacter(party.GetParty()[partyMemberSelect]);
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
                    AwardExperience();
                    SetBattleState(BattleState.Outro);
                }
            }
        }

        private void AwardExperience()
        {
            foreach (CombatParticipant character in GetCharacters())
            {
                Experience experience = character.GetComponent<Experience>();
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