using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Core;
using Frankie.Stats;
using Frankie.Control;

namespace Frankie.Combat
{
    [RequireComponent(typeof(BattleRewards))]
    public class BattleController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] float battleQueueDelay = 1.0f;

        [Header("Positional Properties")]
        [SerializeField] int minRowCount = 2;
        [SerializeField] int maxEnemiesPerRow = 7;
        [SerializeField] int minEnemiesBeforeRowSplit = 2;

        // State
        BattleState battleState;
        bool hasEnteredCombatState = false;
        bool outroCleanupCalled = false;

        private readonly List<BattleEntity> activeCharacters = new List<BattleEntity>();
        private readonly List<BattleEntity> activePlayerCharacters = new List<BattleEntity>();
        private readonly List<BattleEntity> activeAssistCharacters = new List<BattleEntity>();
        private readonly List<BattleEntity> activeEnemies = new List<BattleEntity>();
        private bool[][] enemyMapping;

        private CombatParticipant selectedCharacter;
        private IBattleActionSuper selectedBattleActionSuper;
        private bool battleActionArmed = false;
        private BattleActionData battleActionData;

        private readonly Queue<BattleSequence> battleSequenceQueue = new Queue<BattleSequence>();
        private bool haltBattleQueue = false;
        private bool battleSequenceInProgress = false;

        // Cached References
        private PlayerInput playerInput;
        private PartyCombatConduit partyCombatConduit;
        private BattleRewards battleRewards;

        // Events
        public event Action<PlayerInputType> battleInput;
        public event Action<PlayerInputType> globalInput;

        #region StaticFind
        private const string _battleControllerTag = "BattleController";

        public static BattleController FindBattleController()
        {
            var battleControllerGameObject = GameObject.FindGameObjectWithTag(_battleControllerTag);
            return battleControllerGameObject != null ? battleControllerGameObject.GetComponent<BattleController>() : null;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            playerInput = new PlayerInput();
            partyCombatConduit = Player.FindPlayerObject()?.GetComponent<PartyCombatConduit>();
            battleRewards = GetComponent<BattleRewards>();

            VerifyUnique();

            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Execute.performed += _ => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += _ => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Menu.Option.performed += _ => HandleUserInput(PlayerInputType.Option);
            playerInput.Menu.Skip.performed += _ => HandleUserInput(PlayerInputType.Skip);
            playerInput.Menu.Select1.performed += _ => InteractWithCharacterSelect(0);
            playerInput.Menu.Select2.performed += _ => InteractWithCharacterSelect(1);
            playerInput.Menu.Select3.performed += _ => InteractWithCharacterSelect(2);
            playerInput.Menu.Select4.performed += _ => InteractWithCharacterSelect(3);
        }

        private void OnEnable()
        {
            playerInput.Menu.Enable();
            BattleEventBus<BattleEntityRemovedFromBoardEvent>.SubscribeToEvent(RemoveFromEnemyMapping);
            BattleEventBus<StateAlteredInfo>.SubscribeToEvent(HandleCharacterDeath);
            BattleEventBus<StateAlteredInfo>.SubscribeToEvent(CheckForBattleEnd);
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
            BattleEventBus<BattleEntityRemovedFromBoardEvent>.UnsubscribeFromEvent(RemoveFromEnemyMapping);
            BattleEventBus<StateAlteredInfo>.UnsubscribeFromEvent(HandleCharacterDeath);
            BattleEventBus<StateAlteredInfo>.UnsubscribeFromEvent(CheckForBattleEnd);
        }

        private void Update()
        {
            if (battleState == BattleState.Combat)
            {
                if (!haltBattleQueue && !battleSequenceInProgress)
                {
                    StartCoroutine(ProcessNextBattleSequence());
                }

            }
            else if (battleState == BattleState.Outro)
            {
                if (!outroCleanupCalled)
                {
                    outroCleanupCalled = true;
                    CleanUpBattleBits();
                }
            }
            else if (battleState == BattleState.Complete && outroCleanupCalled) { outroCleanupCalled = false; }
        }

        private void LateUpdate()
        {
            if (battleState == BattleState.Combat)
            {
                AutoSelectCharacter();
            }
        }
        #endregion

        #region PublicSetters
        // Setters
        public void QueueCombatInitiation(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            Fader fader = Fader.FindFader();
            void BattleControllerInitiateTrigger() => InitiateBattle(enemies, transitionType);
            fader.QueueInitiateBattleCallback(BattleControllerInitiateTrigger);
        }

        public void SetBattleState(BattleState state, BattleOutcome battleOutcome)
        {
            this.battleState = state;
            if (!hasEnteredCombatState && state == BattleState.Combat) { hasEnteredCombatState = true; }

            ToggleCombatParticipants(state == BattleState.Combat);

            BattleEventBus<BattleStateChangedEvent>.Raise(new BattleStateChangedEvent(state, battleOutcome));
        }

        public bool SetSelectedCharacter(CombatParticipant character)
        {
            if (character == null) { SetActiveBattleAction(null); }
            if (character != null) { if (character.IsDead() || character.IsInCooldown()) { return false; } }

            selectedCharacter = character;
            battleActionData = selectedCharacter != null ? new BattleActionData(GetSelectedCharacter()) : null;

            List<BattleEntity> selectedBattleEntity = new() { new(selectedCharacter) };
            BattleEventBus<BattleEntitySelectedEvent>.Raise(new BattleEntitySelectedEvent(CombatParticipantType.Friendly, selectedBattleEntity));

            return true;
        }

        public bool SetSelectedTarget(IBattleActionSuper battleActionUser, bool traverseForward = true)
        {
            if (battleActionUser == null)
            {
                if (selectedCharacter != null) { battleActionData = new BattleActionData(selectedCharacter); }
                // Note:  On resetting after skill execution, this will generate a battle action data that gets thrown out
            }
            else
            {
                battleActionUser.GetTargets(traverseForward, battleActionData, activeCharacters, activeEnemies);
            }

            BattleEventBus<BattleEntitySelectedEvent>.Raise(new BattleEntitySelectedEvent(CombatParticipantType.Foe, battleActionData?.GetTargets()));

            if (battleActionData != null)
            {
                if (battleActionData.targetCount == 0) { return false; }

                // Check for whole set dead
                bool allEntitiesDead = true;
                foreach (BattleEntity battleEntity in battleActionData.GetTargets()) { if (!battleEntity.combatParticipant.IsDead()) { allEntitiesDead = false; break; } }
                if (allEntitiesDead) { return false; }
            }

            return true;
        }

        public void SetActiveBattleAction(IBattleActionSuper battleActionSuper)
        {
            if (battleActionSuper == null)
            {
                SetBattleActionArmed(false);
                selectedCharacter?.GetComponent<SkillHandler>().ResetCurrentBranch();
                selectedBattleActionSuper = null;
            }
            else
            {
                selectedBattleActionSuper = battleActionSuper;
            }
        }

        public void SetBattleActionArmed(bool enable)
        {
            if (!enable)
            {
                SetSelectedTarget(null);
                battleActionArmed = false;
            }
            else if (selectedBattleActionSuper != null)
            {
                SetSelectedTarget(selectedBattleActionSuper);
                battleActionArmed = true;
            }
            else { return; }

            BattleEventBus<BattleActionArmedEvent>.Raise(new BattleActionArmedEvent(selectedBattleActionSuper));
        }
        #endregion

        #region PublicGetters
        // Overall State
        public BattleState GetBattleState() => battleState;
        public bool IsStateOutsideCombat() => battleState is BattleState.Inactive or BattleState.Rewards or BattleState.Outro or BattleState.Complete;
        public List<BattleEntity> GetCharacters() => activeCharacters;
        public List<BattleEntity> GetEnemies() => activeEnemies;
        public List<BattleEntity> GetAssistCharacters() => activeAssistCharacters;
        public BattleRewards GetBattleRewards() => battleRewards;
        public bool IsEnemyPositionAvailable()
        {
            foreach (bool[] row in enemyMapping)
            {
                foreach (bool entry in row) { if (!entry) return true; }
            }
            Debug.Log("No remaining positions for enemies to spawn");
            return false;
        }

        // State Selections
        public CombatParticipant GetSelectedCharacter()
        {
            AutoSelectCharacter();
            return selectedCharacter;
        }
        public IBattleActionSuper GetActiveBattleAction() => selectedBattleActionSuper;
        public bool HasActiveBattleAction() => selectedBattleActionSuper != null;
        public bool IsBattleActionArmed() => battleActionArmed;
        #endregion

        #region PublicBattleHandling
        public bool AddToBattleQueue(List<BattleEntity> recipients)
        {
            // Called via SkillSelection UI Buttons
            // Using selected character and battle action
            if (GetSelectedCharacter() == null || selectedBattleActionSuper == null) { return false; }
            battleActionData.SetTargets(recipients);
            selectedBattleActionSuper.GetTargets(null, battleActionData, activeCharacters, activeEnemies); // Select targets with null traverse to apply filters & pass back

            if (battleActionData.targetCount == 0) { return false; }

            AddToBattleQueue(battleActionData, selectedBattleActionSuper);
            return true;
        }

        public void AddToBattleQueue(BattleActionData battleActionDataForQueue, IBattleActionSuper battleActionSuper)
        {
            BattleSequence battleSequence = new BattleSequence
            {
                battleActionSuper = battleActionSuper,
                battleActionData = battleActionDataForQueue
            };
            AddToBattleQueue(battleSequence);
        }

        private void AddToBattleQueue(BattleSequence battleSequence)
        {
            CombatParticipant sender = battleSequence.battleActionData.GetSender();
            sender.SetCooldown(Mathf.Infinity); // Character actions locked until cooldown set by BattleController
            battleSequenceQueue.Enqueue(battleSequence);

            foreach (BattleEntity battleEntity in activePlayerCharacters)
            {
                if (battleEntity.combatParticipant == sender)
                {
                    // Clear out selection on player execution
                    SetActiveBattleAction(null);
                    SetSelectedCharacter(null);
                    break;
                }
            }
        }

        public bool AttemptToRun()
        {
            bool allCharactersAvailable = true;
            float partySpeed = 0f;
            float enemySpeed = 0f;

            // Get Party average speed && check for availability
            foreach (BattleEntity character in activePlayerCharacters)
            {
                allCharactersAvailable = allCharactersAvailable && !character.combatParticipant.IsInCooldown();
                partySpeed += character.combatParticipant.GetRunSpeed();
            }
            if (!hasEnteredCombatState) { allCharactersAvailable = true; } // Override for pre-battle run attempt
            float averagePartySpeed = partySpeed / activePlayerCharacters.Count;

            // Get enemy max speed
            foreach (BattleEntity enemy in activeEnemies)
            {
                enemySpeed = Mathf.Max(enemySpeed, enemy.combatParticipant.GetRunSpeed());
            }

            // Probability via CalculatedStat and check/react
            float runChance = CalculatedStats.GetCalculatedStat(CalculatedStat.RunChance, 0, averagePartySpeed, enemySpeed);
            float runCheck = UnityEngine.Random.value;
            Debug.Log($"Run Attempt.  Run chance @ {runChance}.  Run check @ {runCheck}");
            Debug.Log($"Checking if all characters available: {allCharactersAvailable}");
            if (allCharactersAvailable && (runCheck < runChance))
            {
                foreach (BattleEntity enemy in activeEnemies)
                {
                    enemy.combatParticipant.SetupSelfDestroyOnBattleComplete();
                }
                SetBattleState(BattleState.Outro, BattleOutcome.Ran);
                return true;
            }
            else
            {
                foreach (BattleEntity character in activeCharacters)
                {
                    character.combatParticipant.IncrementCooldownStoreForRun();
                }
                hasEnteredCombatState = true; // Treat run as having entered combat
                return false;
            }
        }
        #endregion

        #region PrivateInteraction
        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            PlayerInputType playerInputType = this.NavigationVectorToInputType(directionalInput);
            HandleUserInput(playerInputType);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            if (battleState == BattleState.Combat)
            {
                if (InteractWithInterrupts(playerInputType)) { return; }
                if (InteractWithSkillSelect(playerInputType)) { return; }
                if (InteractWithBattleActionExecute(playerInputType)) { return; }
            }

            // Final call to globals, avoid short circuit
            // ReSharper disable once RedundantJumpStatement
            if (InteractWithGlobals(playerInputType)) { return; }
        }

        private bool InteractWithInterrupts(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.Cancel)
            {
                // Move to Combat Options if nothing selected in skill selector
                if (selectedBattleActionSuper == null || selectedCharacter == null)
                {
                    SetSelectedCharacter(null);
                    SetBattleState(BattleState.PreCombat, BattleOutcome.Undetermined); return true;
                }

                // Otherwise step out of selections
                if (selectedBattleActionSuper != null || selectedCharacter != null)
                {
                    SetActiveBattleAction(null);
                    SetSelectedCharacter(null);
                    SetBattleState(BattleState.Combat, BattleOutcome.Undetermined);
                }
                return true;
            }
            return false;
        }

        private void InteractWithCharacterSelect(int partyMemberSelect)
        {
            if (GetBattleState() != BattleState.Combat) { return; }
            if (partyMemberSelect >= partyCombatConduit.GetPartyCombatParticipants().Count) { return; } // >= since indexing off by 1 from count

            SetSelectedCharacter(partyCombatConduit.GetPartyCombatParticipants()[partyMemberSelect]);
        }

        private bool InteractWithSkillSelect(PlayerInputType playerInputType)
        {
            if (GetBattleState() != BattleState.Combat) { return false; }
            if (IsBattleActionArmed()) { return false; }

            if (selectedCharacter != null && !selectedCharacter.IsDead())
            {
                if (playerInputType == PlayerInputType.Execute && selectedBattleActionSuper != null)
                {
                    SetBattleActionArmed(true);
                    return true;
                }

                if (playerInputType != PlayerInputType.DefaultNone)
                {
                    battleInput?.Invoke(playerInputType);
                }
            }
            return false;
        }

        private bool InteractWithBattleActionExecute(PlayerInputType playerInputType)
        {

            if (GetBattleState() != BattleState.Combat) { return false; }
            if (!IsBattleActionArmed() || selectedCharacter == null || selectedBattleActionSuper == null) { return false; }

            if (playerInputType != PlayerInputType.DefaultNone)
            {
                if (playerInputType == PlayerInputType.Execute)
                {
                    if (battleActionData.targetCount == 0) { return false; }
                    AddToBattleQueue(battleActionData, selectedBattleActionSuper);
                }
                else
                {
                    if (playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
                    {
                        SetSelectedTarget(selectedBattleActionSuper);
                    }
                    else if (playerInputType == PlayerInputType.NavigateLeft || playerInputType == PlayerInputType.NavigateUp)
                    {
                        SetSelectedTarget(selectedBattleActionSuper, false);
                    }
                }
                battleInput?.Invoke(playerInputType);

                return true;
            }
            return false;
        }

        private bool InteractWithGlobals(PlayerInputType playerInputType)
        {
            if (playerInputType != PlayerInputType.DefaultNone)
            {
                globalInput?.Invoke(playerInputType);
                return true;
            }
            return false;
        }
        #endregion

        #region BattleHandling
        private void InitiateBattle(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            SetupCombatParticipants(enemies, transitionType);
            BattleEventBus<BattleEnterEvent>.Raise(new BattleEnterEvent(activeCharacters, activeEnemies, transitionType));

            if (CheckForAutoWin())
            {
                foreach (BattleEntity enemy in activeEnemies)
                {
                    enemy.combatParticipant.SelfImplode();
                }
                SetBattleState(BattleState.Combat, BattleOutcome.Undetermined); // Combat will auto-complete in the usual way
            }
            else
            {
                SetBattleState(BattleState.Intro, BattleOutcome.Undetermined);
            }
        }

        private void SetupCombatParticipants(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            // Party Characters
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants())
            {
                AddCharacterToCombat(character, transitionType);
            }

            // Enemies
            InitializeEnemyMapping(enemies.Count);
            foreach (CombatParticipant enemy in enemies)
            {
                AddEnemyToCombat(enemy, transitionType);
            }

            // Party Assist Characters
            foreach (CombatParticipant character in partyCombatConduit.GetPartyAssistParticipants())
            {
                AddAssistCharacterToCombat(character, transitionType);
            }
        }

        private void AddCharacterToCombat(CombatParticipant character, TransitionType transitionType)
        {
            character.InitializeCooldown(true, IsBattleAdvantage(true, transitionType));

            BattleEntity characterBattleEntity = new BattleEntity(character);
            activeCharacters.Add(characterBattleEntity);
            activePlayerCharacters.Add(characterBattleEntity);

            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(characterBattleEntity));
        }

        private void AddAssistCharacterToCombat(CombatParticipant character, TransitionType transitionType)
        {
            character.InitializeCooldown(false, IsBattleAdvantage(true, transitionType));

            BattleEntity assistBattleEntity = new BattleEntity(character, true);
            activeCharacters.Add(assistBattleEntity);
            activeAssistCharacters.Add(assistBattleEntity);

            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(assistBattleEntity));
        }

        public void AddEnemyToCombat(CombatParticipant enemy, TransitionType transitionType = TransitionType.BattleNeutral, bool forceCombatActive = false)
        {
            enemy.InitializeCooldown(false, IsBattleAdvantage(false, transitionType));

            GetEnemyPosition(out int rowIndex, out int columnIndex);
            Debug.Log($"New enemy added at position row: {rowIndex} ; col: {columnIndex}");

            BattleEntity enemyBattleEntity = new BattleEntity(enemy, enemy.GetBattleEntityType(), rowIndex, columnIndex);
            activeEnemies.Add(enemyBattleEntity);

            enemy.SetCombatActive(forceCombatActive);
            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(enemyBattleEntity));
        }

        private bool? IsBattleAdvantage(bool isFriendly, TransitionType transitionType)
        {
            bool? isBattleAdvantage = null;
            if (transitionType == TransitionType.BattleGood) { isBattleAdvantage = isFriendly; }
            else if (transitionType == TransitionType.BattleBad) { isBattleAdvantage = !isFriendly; }

            return isBattleAdvantage;
        }

        private int InitializeEnemyMapping(int enemyCount)
        {
            int numberOfRows = Mathf.Max(minRowCount, enemyCount / maxEnemiesPerRow + 1);
            enemyMapping = new bool[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++) { enemyMapping[i] = new bool[maxEnemiesPerRow]; }

            return numberOfRows;
        }

        private void GetEnemyPosition(out int rowIndex, out int columnIndex)
        {
            int numberOfRows = enemyMapping?.Length ?? InitializeEnemyMapping(activeEnemies.Count);
            if (enemyMapping == null) { rowIndex = 0; columnIndex = 0; return; }

            // Find row position
            rowIndex = 0;
            int lastRowLength = enemyMapping[0].Count(c => c);
            if (lastRowLength > minEnemiesBeforeRowSplit) // Populate first row up to quantity, then begin populating second+ rows
            {
                for (int i = 1; i < numberOfRows; i++)
                {
                    int currentRowLength = enemyMapping[i].Count(c => c);
                    if (currentRowLength < lastRowLength) { rowIndex = i; lastRowLength = currentRowLength; }
                }
            }

            // Find column position
            int centerColumn = maxEnemiesPerRow / 2;
            columnIndex = centerColumn;
            if (enemyMapping[rowIndex][centerColumn]) // Center column already populated, loop through
            {
                for (int i = 1; i < centerColumn + 1; i++)
                {
                    if (!enemyMapping[rowIndex][centerColumn - i]) { columnIndex = centerColumn - i; break; } // -1 offset from center
                    if (centerColumn + i >= maxEnemiesPerRow) { break; } // Handling for even counts
                    if (!enemyMapping[rowIndex][centerColumn + i]) { columnIndex = centerColumn + i; break; } // +1 offset from center
                }
            }
            enemyMapping[rowIndex][columnIndex] = true;
        }

        private void RemoveFromEnemyMapping(BattleEntityRemovedFromBoardEvent battleEntityRemovedFromBoardEvent)
        {
            enemyMapping[battleEntityRemovedFromBoardEvent.row][battleEntityRemovedFromBoardEvent.column] = false;
        }

        private bool CheckForAutoWin()
        {
            float imposingStat = 1f;
            foreach (BattleEntity enemy in activeEnemies)
            {
                float subImposingStat = -1f;
                foreach (BattleEntity character in activePlayerCharacters)
                {
                    float newImposingStat = character.combatParticipant.GetCalculatedStat(CalculatedStat.Imposing, enemy.combatParticipant);
                    subImposingStat = newImposingStat > subImposingStat ? newImposingStat : subImposingStat;
                    // Within single enemy, if any character is imposing, set is imposing (>0f) -- take the largest value

                    if (subImposingStat > 0f) { break; } // break early if imposed
                }
                imposingStat = subImposingStat < imposingStat ? subImposingStat : imposingStat;
                // Within set of enemies, if any enemy is not imposed upon, battle will continue -- take the minimum value

                if (imposingStat < 0f) { break; } // break early if not imposed upon
            }

            return imposingStat > 0f;
        }

        private void AutoSelectCharacter()
        {
            if (activePlayerCharacters == null || selectedCharacter != null) { return; }
            CombatParticipant firstFreeCharacter = null;
            foreach (BattleEntity battleEntity in activePlayerCharacters)
            {
                if (!battleEntity.combatParticipant.IsDead() && !battleEntity.combatParticipant.IsInCooldown())
                {
                    firstFreeCharacter = battleEntity.combatParticipant;
                    break;
                }
            }
            if (firstFreeCharacter != null) { SetSelectedCharacter(firstFreeCharacter); }
        }

        private void ToggleCombatParticipants(bool enable)
        {
            foreach (BattleEntity character in activeCharacters)
            {
                character.combatParticipant.SetCombatActive(enable);
            }
            foreach (BattleEntity enemy in activeEnemies)
            {
                enemy.combatParticipant.SetCombatActive(enable);
            }
        }

        IEnumerator ProcessNextBattleSequence()
        {
            if (battleSequenceQueue.Count == 0) { yield break; }
            BattleSequence battleSequence = battleSequenceQueue.Dequeue();
            BattleActionData dequeuedBattleActionData = battleSequence.battleActionData;

            bool allEntitiesDead = true;
            foreach (BattleEntity battleEntity in dequeuedBattleActionData.GetTargets()) { if (!battleEntity.combatParticipant.IsDead()) { allEntitiesDead = false; break; } }
            if (dequeuedBattleActionData.GetSender().IsDead() || allEntitiesDead) { yield break; }

            BattleEventBus<BattleSequenceProcessedEvent>.Raise(new BattleSequenceProcessedEvent(battleSequence));

            // Useful Debug
            //string targetNames = string.Concat(dequeuedBattleActionData.GetTargets().Select(x => x.name));
            //UnityEngine.Debug.Log($"Battle sequence from {dequeuedBattleActionData.GetSender().name} dequeued, for action {battleSequence.battleAction.GetName()} on {targetNames}");

            // Two flags to flip to re-enable battle:
            // A. global battle queue delay, handled by coroutine
            // B. battle sequence, handled by callback (e.g. for handling effects)
            haltBattleQueue = true;
            battleSequenceInProgress = true;

            battleSequence.battleActionSuper.Use(dequeuedBattleActionData, () => { battleSequenceInProgress = false; });
            yield return new WaitForSeconds(battleQueueDelay);

            haltBattleQueue = false;
        }

        private void HandleCharacterDeath(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo.stateAlteredType != StateAlteredType.Dead) { return; }

            if (selectedCharacter == stateAlteredInfo.combatParticipant)
            {
                SetActiveBattleAction(null); SetSelectedCharacter(null);
                return;
            }

            if (battleActionData != null)
            {
                if (battleActionData.targetCount > 0 && battleActionData.HasTarget(stateAlteredInfo.combatParticipant))
                {
                    SetSelectedTarget(selectedBattleActionSuper);
                }
            }
        }

        private void CheckForBattleEnd(StateAlteredInfo stateAlteredInfo)
        {
            if (IsStateOutsideCombat()) { return; }

            if (stateAlteredInfo.stateAlteredType == StateAlteredType.Dead)
            {
                bool allCharactersDead = true;
                foreach (BattleEntity battleEntity in activePlayerCharacters) { if (!battleEntity.combatParticipant.IsDead()) { allCharactersDead = false; break; } }
                if (allCharactersDead)
                {
                    SetBattleState(BattleState.Outro, BattleOutcome.Lost);
                }

                bool allEnemiesDead = true;
                foreach (BattleEntity battleEntity in activeEnemies) { if (!battleEntity.combatParticipant.IsDead()) { allEnemiesDead = false; break; } }
                if (allEnemiesDead)
                {
                    bool rewardsExist = battleRewards.HandleBattleRewardsTriggered(partyCombatConduit, activePlayerCharacters, GetEnemies());
                    BattleState checkBattleState = rewardsExist ? BattleState.Rewards : BattleState.Outro;
                    SetBattleState(checkBattleState, BattleOutcome.Won);
                }
            }
        }

        private void CleanUpBattleBits()
        {
            foreach (BattleEntity character in activeCharacters)
            {
                character.combatParticipant.SetCombatActive(false);
            }
            foreach (BattleEntity enemy in activeEnemies)
            {
                enemy.combatParticipant.SetCombatActive(false);
            }
            activeCharacters.Clear();
            activePlayerCharacters.Clear();
            activeAssistCharacters.Clear();
            activeEnemies.Clear();
            SetSelectedCharacter(null);
        }
        #endregion

        #region Interfaces
        public void VerifyUnique()
        {
            BattleController[] battleControllers = FindObjectsByType<BattleController>(FindObjectsSortMode.None);
            if (battleControllers.Length > 1)
            {
                Destroy(gameObject);
            }
        }

        public PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector)
        {
            // Not evaluated -> IStandardPlayerInputCallerExtension
            return PlayerInputType.DefaultNone;
        }
        #endregion
    }
}
