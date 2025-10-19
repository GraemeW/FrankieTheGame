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
        [SerializeField] private float battleQueueDelay = 1.0f;

        [Header("Positional Properties")]
        [SerializeField] private int maxEnemiesPerRow = 8;
        [SerializeField] private int minEnemiesBeforeRowSplit = 2;

        // Fixed
        private readonly HashSet<BattleRow> defaultBattleRowPriority = new () { BattleRow.Middle, BattleRow.Top };
        
        // State
        private BattleState battleState;
        private bool canAttemptEarlyRun = true;
        private bool outroCleanupCalled = false;

        private readonly List<BattleEntity> activeCharacters = new();
        private readonly List<BattleEntity> activePlayerCharacters = new();
        int countEnemiesAddedMidCombat = 0;
        private readonly List<BattleEntity> activeAssistCharacters = new();
        private readonly List<BattleEntity> activeEnemies = new();
        private readonly Dictionary<BattleRow, int> enemyMap = new() { {BattleRow.Middle, 0}, {BattleRow.Top, 0}, {BattleRow.Bottom, 0} };

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

        public static bool IsCombatParticipantAvailableToAct(CombatParticipant combatParticipant)
        {
            return !combatParticipant.IsDead() && !combatParticipant.IsInCooldown();
        }
        
        private static bool? IsBattleAdvantage(bool isFriendly, TransitionType transitionType)
        {
            bool? isBattleAdvantage = null;
            if (transitionType == TransitionType.BattleGood) { isBattleAdvantage = isFriendly; }
            else if (transitionType == TransitionType.BattleBad) { isBattleAdvantage = !isFriendly; }

            return isBattleAdvantage;
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
            BattleEventBus<BattleEntitySelectedEvent>.SubscribeToEvent(HandleBattleEntitySelectedEvent);
            BattleEventBus<BattleQueueUpdatedEvent>.SubscribeToEvent(HandleBattleQueueUpdatedEvent);
            BattleEventBus<BattleEntityRemovedFromBoardEvent>.SubscribeToEvent(RemoveFromEnemyMapping);
            BattleEventBus<StateAlteredInfo>.SubscribeToEvent(HandleCharacterDeath);
            BattleEventBus<StateAlteredInfo>.SubscribeToEvent(CheckForBattleEnd);
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
            BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(HandleBattleEntitySelectedEvent);
            BattleEventBus<BattleQueueUpdatedEvent>.UnsubscribeFromEvent(HandleBattleQueueUpdatedEvent);
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
            battleState = state;
            if (canAttemptEarlyRun && state == BattleState.Combat) { canAttemptEarlyRun = false; }

            BattleEventBus<BattleStateChangedEvent>.Raise(new BattleStateChangedEvent(state, battleOutcome, activeCharacters.AsReadOnly(), activeEnemies.AsReadOnly()));
        }
        
        public bool SetSelectedCharacter(CombatParticipant character)
        {
            if (character == null) { ClearSelectedCharacter(); return true; }
            if (!IsCombatParticipantAvailableToAct(character)) { return false; }

            selectedCharacter = character;
            List<BattleEntity> selectedBattleEntity = new() { new(selectedCharacter) };
            BattleEventBus<BattleEntitySelectedEvent>.Raise(new BattleEntitySelectedEvent(CombatParticipantType.Friendly, selectedBattleEntity));

            return true;
        }

        private bool SetSelectedTarget(IBattleActionSuper battleActionUser, bool traverseForward = true)
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
                bool allEntitiesDead = battleActionData.GetTargets().All(battleEntity => battleEntity.combatParticipant.IsDead());
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
            
            BattleEventBus<BattleActionSelectedEvent>.Raise(new BattleActionSelectedEvent(selectedBattleActionSuper));
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
        public BattleRewards GetBattleRewards() => battleRewards;
        public int GetCountEnemiesAddedMidCombat() => countEnemiesAddedMidCombat;
        public bool IsEnemyPositionAvailable()
        {
            if (defaultBattleRowPriority.Any(battleRow => GetEnemyCountInRow(battleRow) < maxEnemiesPerRow)) { return true; }
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
        public bool IsBattleActionArmed() => battleActionArmed;
        #endregion

        #region EventBusHandlers
        private void HandleBattleEntitySelectedEvent(BattleEntitySelectedEvent battleEntitySelectedEvent)
        {
            if (battleEntitySelectedEvent.combatParticipantType != CombatParticipantType.Friendly) { return; }

            BattleEntity battleEntity = battleEntitySelectedEvent.battleEntities.FirstOrDefault();
            if (battleEntity != null)
            {
                selectedCharacter = battleEntity.combatParticipant; // N.B. Set here too for UI button click support
                battleActionData = new BattleActionData(battleEntity.combatParticipant);
            }
        }
        
        private void HandleBattleQueueUpdatedEvent(BattleQueueUpdatedEvent battleQueueUpdatedEvent)
        {
            BattleSequence battleSequence =  battleQueueUpdatedEvent.battleSequence;
            CombatParticipant sender = battleSequence.battleActionData.GetSender();
            sender.SetCooldown(Mathf.Infinity); // Character actions locked until cooldown set by BattleController
            battleSequenceQueue.Enqueue(battleSequence);

            if (activePlayerCharacters.Any(battleEntity => battleEntity.combatParticipant == sender))
            {
                SetActiveBattleAction(null);
                ClearSelectedCharacter();
            }
        }
        
        private void RemoveFromEnemyMapping(BattleEntityRemovedFromBoardEvent battleEntityRemovedFromBoardEvent)
        {
            SetEnemyInMap(battleEntityRemovedFromBoardEvent.row, battleEntityRemovedFromBoardEvent.column, false);
        }
        
        private void HandleCharacterDeath(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo.stateAlteredType != StateAlteredType.Dead) { return; }

            if (selectedCharacter == stateAlteredInfo.combatParticipant)
            {
                SetActiveBattleAction(null); ClearSelectedCharacter();
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
            if (battleState is BattleState.Inactive or BattleState.Rewards or BattleState.Outro or BattleState.Complete) { return; }
            if (stateAlteredInfo.stateAlteredType != StateAlteredType.Dead) return;
            
            bool allCharactersDead = activePlayerCharacters.All(battleEntity => battleEntity.combatParticipant.IsDead());
            if (allCharactersDead)
            {
                SetBattleState(BattleState.Outro, BattleOutcome.Lost);
            }

            bool allEnemiesDead = activeEnemies.All(battleEntity => battleEntity.combatParticipant.IsDead());
            if (allEnemiesDead)
            {
                bool rewardsExist = battleRewards.HandleBattleRewardsTriggered(partyCombatConduit, activePlayerCharacters, activeEnemies);
                BattleState checkBattleState = rewardsExist ? BattleState.Rewards : BattleState.Outro;
                SetBattleState(checkBattleState, BattleOutcome.Won);
            }
        }
        #endregion
        
        #region PublicBattleHandling
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
            if (canAttemptEarlyRun) { allCharactersAvailable = true; } // Override for pre-battle run attempt
            float averagePartySpeed = partySpeed / activePlayerCharacters.Count;

            // Get enemy max speed
            foreach (BattleEntity enemy in activeEnemies) { enemySpeed = Mathf.Max(enemySpeed, enemy.combatParticipant.GetRunSpeed()); }

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
                canAttemptEarlyRun = false; // Treat run as having entered combat
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
                    ClearSelectedCharacter();
                    SetBattleState(BattleState.PreCombat, BattleOutcome.Undetermined); return true;
                }

                // Otherwise step out of selections
                if (selectedBattleActionSuper != null || selectedCharacter != null)
                {
                    SetActiveBattleAction(null);
                    ClearSelectedCharacter();
                    SetBattleState(BattleState.Combat, BattleOutcome.Undetermined);
                }
                return true;
            }
            return false;
        }

        private void InteractWithCharacterSelect(int partyMemberSelect)
        {
            if (battleState != BattleState.Combat) { return; }
            if (partyMemberSelect >= partyCombatConduit.GetPartyCombatParticipants().Count) { return; } // >= since indexing off by 1 from count

            SetSelectedCharacter(partyCombatConduit.GetPartyCombatParticipants()[partyMemberSelect]);
        }

        private bool InteractWithSkillSelect(PlayerInputType playerInputType)
        {
            if (battleState != BattleState.Combat) { return false; }
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
            if (battleState != BattleState.Combat) { return false; }
            if (!IsBattleActionArmed() || selectedCharacter == null || selectedBattleActionSuper == null) { return false; }

            if (playerInputType != PlayerInputType.DefaultNone)
            {
                if (playerInputType == PlayerInputType.Execute)
                {
                    if (battleActionData.targetCount == 0) { return false; }
                    
                    var battleSequence = new BattleSequence(selectedBattleActionSuper, battleActionData);
                    BattleEventBus<BattleQueueUpdatedEvent>.Raise(new BattleQueueUpdatedEvent(battleSequence));
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
            if (transitionType == TransitionType.BattleBad) { canAttemptEarlyRun = false; }
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
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants())
            {
                AddCharacterToCombat(character, transitionType);
            }
            
            foreach (CombatParticipant enemy in enemies)
            {
                AddEnemyToCombat(enemy, transitionType);
            }
            
            foreach (CombatParticipant character in partyCombatConduit.GetPartyAssistParticipants())
            {
                AddAssistCharacterToCombat(character, transitionType);
            }
        }

        private void AddCharacterToCombat(CombatParticipant character, TransitionType transitionType)
        {
            character.InitializeCooldown(true, IsBattleAdvantage(true, transitionType));
            character.SubscribeToBattleStateChanges(true);

            BattleEntity characterBattleEntity = new BattleEntity(character);
            activeCharacters.Add(characterBattleEntity);
            activePlayerCharacters.Add(characterBattleEntity);

            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(characterBattleEntity, false));
        }

        private void AddAssistCharacterToCombat(CombatParticipant character, TransitionType transitionType)
        {
            character.InitializeCooldown(false, IsBattleAdvantage(true, transitionType));
            character.SubscribeToBattleStateChanges(true);

            BattleEntity assistBattleEntity = new BattleEntity(character, true);
            activeCharacters.Add(assistBattleEntity);
            activeAssistCharacters.Add(assistBattleEntity);

            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(assistBattleEntity, false));
        }

        public void AddEnemyToCombat(CombatParticipant enemy, TransitionType transitionType = TransitionType.BattleNeutral, bool addMidCombatForceActive = false)
        {
            enemy.InitializeCooldown(false, IsBattleAdvantage(false, transitionType));
            enemy.SubscribeToBattleStateChanges(true);

            BattleRow battleRow = enemy.GetPreferredBattleRow();
            GetEnemyPosition(ref battleRow, out int columnIndex);
            if (battleRow == BattleRow.Any) { Debug.Log($"Warning, could not add {enemy.name} to combat"); return; }
            
            Debug.Log($"New enemy added at position row: {battleRow} ; col: {columnIndex}");

            BattleEntity enemyBattleEntity = new BattleEntity(enemy, enemy.GetBattleEntityType(), battleRow, columnIndex);
            activeEnemies.Add(enemyBattleEntity);

            if (addMidCombatForceActive)
            {
                countEnemiesAddedMidCombat++;
                SetBattleState(BattleState.Combat, BattleOutcome.Undetermined);
            }
            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(enemyBattleEntity, true));
        }

        private void ClearSelectedCharacter()
        {
            SetActiveBattleAction(null);
            selectedCharacter = null;
            battleActionData = null;
            List<BattleEntity> emptyBattleEntity = new() { new(null) };
            BattleEventBus<BattleEntitySelectedEvent>.Raise(new BattleEntitySelectedEvent(CombatParticipantType.Friendly, emptyBattleEntity));
        }

        private int GetEnemyCountInRow(BattleRow battleRow)
        {
            if (battleRow == BattleRow.Any) { return 0; }
            
            // Same as PopCount:  https://learn.microsoft.com/en-us/dotnet/api/system.numerics.bitoperations.popcount?view=net-9.0
            // This implementation used since BitOperations not exposed in Unity's version of C# (non-Core)
            int rowMask = enemyMap[battleRow];
            int rowEnemyCount = 0;
            while (rowMask!=0) { rowMask &= (rowMask-1); rowEnemyCount++; } // n&(n-1) always eliminates the least significant 1

            return rowEnemyCount;
        }

        private List<BattleRow> GetOptimalBattleRowPriority(BattleRow desiredBattleRow)
        {
            List<BattleRow> optimalBattleRowPriority = new();
            if (desiredBattleRow != BattleRow.Any)
            {
                optimalBattleRowPriority.Add(desiredBattleRow); 
                defaultBattleRowPriority.Add(desiredBattleRow); // E.g. Default 2-row @ Mid/Top, new char prefers bott -> thus enables 3-row w/ bott as a default option
            }
            optimalBattleRowPriority.AddRange(defaultBattleRowPriority.Where(testBattleRow => testBattleRow != desiredBattleRow));

            return optimalBattleRowPriority;
        }

        private bool IsEnemyPresent(BattleRow battleRow, int columnIndex)
        {
            int mask = 1 << columnIndex;
            return (enemyMap[battleRow] & mask) != 0;
        }

        private void SetEnemyInMap(BattleRow battleRow, int columnIndex, bool enable)
        {
            int mask = 1 << columnIndex;
            if (enable) { enemyMap[battleRow] |= mask; }
            else { enemyMap[battleRow] &= ~mask; }
        }
        
        private void GetEnemyPosition(ref BattleRow battleRow, out int columnIndex)
        {
            List<BattleRow> optimalBattleRowPriority = GetOptimalBattleRowPriority(battleRow);
            optimalBattleRowPriority.RemoveAll(testBattleRow => GetEnemyCountInRow(testBattleRow) >= maxEnemiesPerRow);

            if (optimalBattleRowPriority.Count == 0) { battleRow = BattleRow.Any; columnIndex = 0; return; } // early exit, no rows available
            if (!optimalBattleRowPriority.Contains(battleRow)) { battleRow = BattleRow.Any; } // desired row not available, swap to any
            
            if (battleRow == BattleRow.Any)
            {
                battleRow = GetEnemyCountInRow(optimalBattleRowPriority[0]) <= minEnemiesBeforeRowSplit ? optimalBattleRowPriority[0] 
                    : optimalBattleRowPriority.OrderBy(GetEnemyCountInRow).ToList().FirstOrDefault();
            }

            // Find column position
            int centerColumn = maxEnemiesPerRow / 2;
            columnIndex = centerColumn;
            
            if (IsEnemyPresent(battleRow, columnIndex)) // Center already populated, loop through
            {
                for (int i = 1; i < centerColumn + 1; i++)
                {
                    if (!IsEnemyPresent(battleRow, centerColumn - i)) { columnIndex = centerColumn - i; break; } // -1 offset from center
                    if (centerColumn + i >= maxEnemiesPerRow) { break; } // Handling for even counts
                    if (!IsEnemyPresent(battleRow, centerColumn + i)) { columnIndex = centerColumn + i; break; } // +1 offset from center
                }
            }
            
            SetEnemyInMap(battleRow, columnIndex, true);
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

        private void CleanUpBattleBits()
        {
            activeCharacters.Clear();
            activePlayerCharacters.Clear();
            activeAssistCharacters.Clear();
            activeEnemies.Clear();
            ClearSelectedCharacter();
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
