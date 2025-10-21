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
    [RequireComponent(typeof(BattleMat))]
    public class BattleController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] private float battleQueueDelay = 1.0f;
        
        // State
        private BattleState battleState;
        private bool canAttemptEarlyRun = true;
        private bool outroCleanupCalled = false;
        private int countEnemiesAddedMidCombat;

        private CombatParticipant selectedCharacter;
        private IBattleActionSuper selectedBattleActionSuper;
        private bool battleActionArmed = false;
        private BattleActionData battleActionData;

        private readonly Queue<BattleSequence> battleSequenceQueue = new();
        private bool haltBattleQueue = false;
        private bool battleSequenceInProgress = false;

        // Cached References
        private PlayerInput playerInput;
        private PartyCombatConduit partyCombatConduit;
        private BattleMat battleMat;
        private BattleRewards battleRewards;

        // Events
        public event Action<PlayerInputType> battleInput;
        public event Action<PlayerInputType> globalInput;

        #region StaticMethods
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
        
        public static bool? IsBattleAdvantage(bool isFriendly, TransitionType transitionType)
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
            battleMat = GetComponent<BattleMat>();
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
            BattleEventBus<BattleQueueAddAttemptEvent>.SubscribeToEvent(HandleBattleQueueAddAttemptEvent);
            BattleEventBus<BattleQueueUpdatedEvent>.SubscribeToEvent(HandleBattleQueueUpdatedEvent);
            BattleEventBus<BattleEntityRemovedFromBoardEvent>.SubscribeToEvent(RemoveFromEnemyMapping);
            BattleEventBus<StateAlteredInfo>.SubscribeToEvent(HandleCharacterDeath);
            BattleEventBus<StateAlteredInfo>.SubscribeToEvent(CheckForBattleEnd);
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
            BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(HandleBattleEntitySelectedEvent);
            BattleEventBus<BattleQueueAddAttemptEvent>.UnsubscribeFromEvent(HandleBattleQueueAddAttemptEvent);
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

            BattleEventBus<BattleStateChangedEvent>.Raise(new BattleStateChangedEvent(state, battleOutcome, battleMat.GetActiveCharacters(), battleMat.GetActiveEnemies()));
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
                battleActionUser.GetTargets(traverseForward, battleActionData, battleMat.GetActiveCharacters(), battleMat.GetActiveEnemies());
            }
            BattleEventBus<BattleEntitySelectedEvent>.Raise(new BattleEntitySelectedEvent(CombatParticipantType.Foe, battleActionData?.GetTargets()));

            // Check for whole set dead
            if (battleActionData == null || battleActionData.targetCount == 0) return false;
            return !battleActionData.GetTargets().All(battleEntity => battleEntity.combatParticipant.IsDead());
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
        public bool IsEnemyPositionAvailable() => battleMat.IsEnemyPositionAvailable();

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

        private void HandleBattleQueueAddAttemptEvent(BattleQueueAddAttemptEvent battleQueueAddAttemptEvent)
        {
            if (selectedCharacter == null || selectedBattleActionSuper == null) { return; }
            if (battleQueueAddAttemptEvent.targets == null || battleQueueAddAttemptEvent.targets.Count == 0) { return; }
            if (!IsCombatParticipantAvailableToAct(selectedCharacter)) { return; }
            
            battleActionData = new BattleActionData(selectedCharacter); 
            battleActionData.SetTargets(battleQueueAddAttemptEvent.targets);
            selectedBattleActionSuper.GetTargets(null, battleActionData, battleMat.GetActiveCharacters(), battleMat.GetActiveEnemies()); // Select targets with null traverse to apply filters & pass back
            if (battleActionData.targetCount == 0) { return; }
            
            var battleSequence = new BattleSequence(selectedBattleActionSuper, battleActionData);
            BattleEventBus<BattleQueueUpdatedEvent>.Raise(new BattleQueueUpdatedEvent(battleSequence));
        }
        
        private void HandleBattleQueueUpdatedEvent(BattleQueueUpdatedEvent battleQueueUpdatedEvent)
        {
            BattleSequence battleSequence =  battleQueueUpdatedEvent.battleSequence;
            CombatParticipant sender = battleSequence.battleActionData.GetSender();
            sender.SetCooldown(Mathf.Infinity); // Character actions locked until cooldown set by BattleController
            battleSequenceQueue.Enqueue(battleSequence);

            if (battleMat.GetActivePlayerCharacters().Any(battleEntity => battleEntity.combatParticipant == sender))
            {
                SetActiveBattleAction(null);
                ClearSelectedCharacter();
            }
        }
        
        private void RemoveFromEnemyMapping(BattleEntityRemovedFromBoardEvent battleEntityRemovedFromBoardEvent)
        {
            battleMat.SetEnemyInMap(battleEntityRemovedFromBoardEvent.row, battleEntityRemovedFromBoardEvent.column, false);
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
            
            bool allCharactersDead = battleMat.GetActivePlayerCharacters().All(battleEntity => battleEntity.combatParticipant.IsDead());
            if (allCharactersDead)
            {
                SetBattleState(BattleState.Outro, BattleOutcome.Lost);
            }

            bool allEnemiesDead = battleMat.GetActiveEnemies().All(battleEntity => battleEntity.combatParticipant.IsDead());
            if (allEnemiesDead)
            {
                bool rewardsExist = battleRewards.HandleBattleRewardsTriggered(partyCombatConduit, battleMat.GetActivePlayerCharacters(), battleMat.GetActiveEnemies());
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
            foreach (BattleEntity character in battleMat.GetActivePlayerCharacters())
            {
                allCharactersAvailable = allCharactersAvailable && !character.combatParticipant.IsInCooldown();
                partySpeed += character.combatParticipant.GetRunSpeed();
            }
            if (canAttemptEarlyRun) { allCharactersAvailable = true; } // Override for pre-battle run attempt
            float averagePartySpeed = partySpeed / battleMat.GetCountActivePlayerCharacters();

            // Get enemy max speed
            foreach (BattleEntity enemy in battleMat.GetActiveEnemies()) { enemySpeed = Mathf.Max(enemySpeed, enemy.combatParticipant.GetRunSpeed()); }

            // Probability via CalculatedStat and check/react
            float runChance = CalculatedStats.GetCalculatedStat(CalculatedStat.RunChance, 0, averagePartySpeed, enemySpeed);
            float runCheck = UnityEngine.Random.value;
            Debug.Log($"Run Attempt.  Run chance @ {runChance}.  Run check @ {runCheck}");
            Debug.Log($"Checking if all characters available: {allCharactersAvailable}");
            if (allCharactersAvailable && (runCheck < runChance))
            {
                foreach (BattleEntity enemy in battleMat.GetActiveEnemies())
                {
                    enemy.combatParticipant.SetupSelfDestroyOnBattleComplete();
                }
                SetBattleState(BattleState.Outro, BattleOutcome.Ran);
                return true;
            }
            else
            {
                foreach (BattleEntity character in battleMat.GetActiveCharacters())
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
            BattleEventBus<BattleEnterEvent>.Raise(new BattleEnterEvent(battleMat.GetActiveCharacters(), battleMat.GetActiveEnemies(), transitionType));

            if (CheckForAutoWin())
            {
                foreach (BattleEntity enemy in battleMat.GetActiveEnemies())
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
                battleMat.AddCharacterToCombat(character, transitionType);
            }
            
            foreach (CombatParticipant enemy in enemies)
            {
                battleMat.AddEnemyToCombat(enemy, transitionType);
            }
            
            foreach (CombatParticipant character in partyCombatConduit.GetPartyAssistParticipants())
            {
                battleMat.AddAssistCharacterToCombat(character, transitionType);
            }
        }

        public void AddEnemyMidCombat(CombatParticipant enemy, TransitionType transitionType = TransitionType.BattleNeutral)
        {
            battleMat.AddEnemyToCombat(enemy, transitionType, true);
            countEnemiesAddedMidCombat++;
            SetBattleState(BattleState.Combat, BattleOutcome.Undetermined);
        }

        private void ClearSelectedCharacter()
        {
            SetActiveBattleAction(null);
            selectedCharacter = null;
            battleActionData = null;
            List<BattleEntity> emptyBattleEntity = new() { new(null) };
            BattleEventBus<BattleEntitySelectedEvent>.Raise(new BattleEntitySelectedEvent(CombatParticipantType.Friendly, emptyBattleEntity));
        }

        private bool CheckForAutoWin()
        {
            float imposingStat = 1f;
            foreach (BattleEntity enemy in battleMat.GetActiveEnemies())
            {
                float subImposingStat = -1f;
                foreach (BattleEntity character in battleMat.GetActivePlayerCharacters())
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
            if (battleMat.GetCountActivePlayerCharacters() == 0 || selectedCharacter != null) { return; }
            CombatParticipant firstFreeCharacter = (from battleEntity in battleMat.GetActivePlayerCharacters() where IsCombatParticipantAvailableToAct(battleEntity.combatParticipant) select battleEntity.combatParticipant).FirstOrDefault();
            if (firstFreeCharacter != null) { SetSelectedCharacter(firstFreeCharacter); }
        }

        private IEnumerator ProcessNextBattleSequence()
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
            battleMat.ClearBattleEntities();
            ClearSelectedCharacter();
        }
        #endregion

        #region Interfaces
        public void VerifyUnique()
        {
            var battleControllers = FindObjectsByType<BattleController>(FindObjectsSortMode.None);
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
