using Frankie.Core;
using Frankie.Stats;
using Frankie.Control;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Inventory;
using UnityEngine.TextCore.Text;

namespace Frankie.Combat
{
    public class BattleController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] float battleQueueDelay = 1.0f;
        [SerializeField] float runFailureCooldown = 3.0f;
        [SerializeField] float battleAdvantageCooldown = 0.25f;
        [Header("Positional Properties")]
        [SerializeField] int minRowCount = 2;
        [SerializeField] int maxEnemiesPerRow = 7;
        [SerializeField] int minEnemiesBeforeRowSplit = 2;

        // State
        BattleState state = default;
        BattleOutcome outcome = default;
        bool outroCleanupCalled = false;
        float battleExperienceReward = 0f;

        List<BattleEntity> activeCharacters = new List<BattleEntity>();
        List<BattleEntity> activePlayerCharacters = new List<BattleEntity>();
        List<BattleEntity> activeAssistCharacters = new List<BattleEntity>();
        List<BattleEntity> activeEnemies = new List<BattleEntity>();
        CombatParticipant selectedCharacter = null;
        IBattleActionSuper selectedBattleActionSuper = null;
        bool battleActionArmed = false;
        BattleActionData battleActionData = null;

        Queue<BattleSequence> battleSequenceQueue = new Queue<BattleSequence>();
        bool haltBattleQueue = false;
        bool battleSequenceInProgress = false;

        List<Tuple<string,InventoryItem>> allocatedLootCart = new List<Tuple<string,InventoryItem>>();
        List<Tuple<string, InventoryItem>> unallocatedLootCart = new List<Tuple<string, InventoryItem>>();

        // Cached References
        PlayerInput playerInput = null;
        PartyCombatConduit partyCombatConduit = null;

        // Events
        public event Action<PlayerInputType> battleInput;
        public event Action<PlayerInputType> globalInput;
        public event Action<BattleState> battleStateChanged;
        public event Action<CombatParticipantType, IEnumerable<BattleEntity>> selectedCombatParticipantChanged;
        public event Action<IBattleActionSuper> battleActionArmedStateChanged;
        public event Action<BattleSequence> battleSequenceProcessed;

        // Interaction
        #region UnityMethods
        private void Awake()
        {
            partyCombatConduit = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PartyCombatConduit>();
            playerInput = new PlayerInput();

            VerifyUnique();
            
            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Execute.performed += context => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += context => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Menu.Option.performed += context => HandleUserInput(PlayerInputType.Option);
            playerInput.Menu.Skip.performed += context => HandleUserInput(PlayerInputType.Skip);
            playerInput.Menu.Select1.performed += context => InteractWithCharacterSelect(0);
            playerInput.Menu.Select2.performed += context => InteractWithCharacterSelect(1);
            playerInput.Menu.Select3.performed += context => InteractWithCharacterSelect(2);
            playerInput.Menu.Select4.performed += context => InteractWithCharacterSelect(3);
        }

        private void OnEnable()
        {
            playerInput.Menu.Enable();
            SubscribeToCharacters(true);
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
            SubscribeToCharacters(false);
        }

        private void SubscribeToCharacters(bool enable)
        {
            if (enable)
            {
                foreach (BattleEntity character in activePlayerCharacters)
                {
                    character.combatParticipant.stateAltered += HandleCharacterDeath;
                }
            }
            else
            {
                foreach (BattleEntity character in activePlayerCharacters)
                {
                    character.combatParticipant.stateAltered -= HandleCharacterDeath;
                }
            }
        }

        private void Update()
        {
            if (state == BattleState.Combat)
            {
                if (!haltBattleQueue && !battleSequenceInProgress) // BattleQueue takes priority, avoid user interaction stalling action queue
                {
                    StartCoroutine(ProcessNextBattleSequence());
                }

            }
            else if (state == BattleState.Outro)
            {
                if (!outroCleanupCalled)
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
        #endregion

        #region PublicSetters
        // Setters
        public void Setup(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            // Party Characters
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants())
            {
                if (transitionType == TransitionType.BattleGood)
                {
                    character.SetCooldown(battleAdvantageCooldown);
                }
                else
                {
                    character.SetCooldown(character.GetBattleStartCooldown());
                }
                character.stateAltered += CheckForBattleEnd;
                BattleEntity characterBattleEntity = new BattleEntity(character);
                activeCharacters.Add(characterBattleEntity);
                activePlayerCharacters.Add(characterBattleEntity);
            }
            if (gameObject.activeSelf) { SubscribeToCharacters(true); }

            // Enemies
            List<BattleEntity> enemyBattleEntities = new List<BattleEntity>();
            
            int numberOfRows = Mathf.Max(minRowCount, enemies.Count / maxEnemiesPerRow + 1);
            bool[][] enemyMapping = new bool[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++) { enemyMapping[i] = new bool[maxEnemiesPerRow]; }

            foreach (CombatParticipant enemy in enemies)
            {
                int rowIndex, columnIndex;
                enemyMapping = GetEnemyPosition(numberOfRows, enemyMapping, out rowIndex, out columnIndex);

                if (transitionType == TransitionType.BattleBad)
                {
                    enemy.SetCooldown(battleAdvantageCooldown);
                }
                else
                {
                    enemy.SetCooldown(enemy.GetBattleStartCooldown());
                }
                enemy.stateAltered += CheckForBattleEnd;

                BattleEntity enemyBattleEntity = new BattleEntity(enemy, enemy.GetBattleEntityType(), rowIndex, columnIndex);
                activeEnemies.Add(enemyBattleEntity);
            }

            // Party Assist Characters
            foreach (CombatParticipant character in partyCombatConduit.GetPartyAssistParticipants())
            {
                if (transitionType == TransitionType.BattleGood)
                {
                    character.SetCooldown(battleAdvantageCooldown);
                }
                else
                {
                    character.SetCooldown(character.GetBattleStartCooldown());
                }
                character.stateAltered += CheckForBattleEnd;

                BattleEntity assistBattleEntity = new BattleEntity(character);
                activeCharacters.Add(assistBattleEntity);
                activeAssistCharacters.Add(assistBattleEntity);
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

            battleStateChanged?.Invoke(state);
        }

        public void SetBattleOutcome(BattleOutcome outcome)
        {
            this.outcome = outcome;

            if (outcome == BattleOutcome.Ran)
            {
                DestroyTransientEnemies();
            }
        }

        public bool SetSelectedCharacter(CombatParticipant character)
        {
            if (character == null) { SetActiveBattleAction(null); }
            if (character != null) { if (character.IsDead() || character.IsInCooldown()) { return false; } }

            selectedCharacter = character;
            if (selectedCharacter != null)
            {
                battleActionData = new BattleActionData(GetSelectedCharacter());
            }
            else
            {
                battleActionData = null;
            }
            selectedCombatParticipantChanged?.Invoke(CombatParticipantType.Friendly, new[] { new BattleEntity(selectedCharacter) });

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

            selectedCombatParticipantChanged?.Invoke(CombatParticipantType.Foe, battleActionData?.GetTargets());

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
                SetSelectedTarget(selectedBattleActionSuper, true);
                battleActionArmed = true;
            }
            else { return; }

            battleActionArmedStateChanged?.Invoke(selectedBattleActionSuper);
        }
        #endregion

        #region PublicGetters
        // Overall State
        public BattleState GetBattleState() => state;
        public BattleOutcome GetBattleOutcome() => outcome;
        public List<BattleEntity> GetCharacters() => activeCharacters;
        public List<BattleEntity> GetEnemies() => activeEnemies;
        public List<BattleEntity> GetAssistCharacters() => activeAssistCharacters;

        // State Selections
        public CombatParticipant GetSelectedCharacter()
        {
            AutoSelectCharacter();
            return selectedCharacter;
        }
        public IBattleActionSuper GetActiveBattleAction() => selectedBattleActionSuper;
        public bool HasActiveBattleAction() => selectedBattleActionSuper != null;
        public bool IsBattleActionArmed() => battleActionArmed;

        // Loot
        public float GetBattleExperienceReward() => battleExperienceReward;
        public List<Tuple<string, InventoryItem>> GetAllocatedLootCart() => allocatedLootCart;
        public List<Tuple<string, InventoryItem>> GetUnallocatedLootCart() => unallocatedLootCart;
        public bool HasLootCart() => HasAllocatedLootCart() || HasUnallocatedLootCart();
        public bool HasAllocatedLootCart() => allocatedLootCart != null && allocatedLootCart.Count > 0;
        public bool HasUnallocatedLootCart() => unallocatedLootCart != null && unallocatedLootCart.Count > 0;
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

        public void AddToBattleQueue(BattleActionData battleActionData, IBattleActionSuper battleActionSuper)
        {
            BattleSequence battleSequence = new BattleSequence
            {
                battleActionSuper = battleActionSuper,
                battleActionData = battleActionData
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
            float partySpeed = 0f;
            float enemySpeed = 0f;

            foreach (BattleEntity character in activePlayerCharacters)
            {
                partySpeed += character.combatParticipant.GetRunSpeed();
            }

            foreach (BattleEntity enemy in activeEnemies)
            {
                enemySpeed += enemy.combatParticipant.GetRunSpeed();
            }

            if (partySpeed > enemySpeed)
            {
                SetBattleOutcome(BattleOutcome.Ran);
                SetBattleState(BattleState.Outro);
                return true;
            }
            else
            {
                foreach (BattleEntity character in activeCharacters)
                {
                    if (character.combatParticipant.GetCooldown() < runFailureCooldown)
                    {
                        character.combatParticipant.SetCooldown(runFailureCooldown);
                    }
                }
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
            if (state == BattleState.Combat)
            {
                if (InteractWithInterrupts(playerInputType)) { return; }
                if (InteractWithSkillSelect(playerInputType)) { return; }
                if (InteractWithBattleActionExecute(playerInputType)) { return; }
            }

            // Final call to globals, avoid short circuit
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
                    SetBattleState(BattleState.PreCombat); return true;
                }

                // Otherwise step out of selections
                if (selectedBattleActionSuper != null || selectedCharacter != null)
                {
                    SetActiveBattleAction(null);
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
                        SetSelectedTarget(selectedBattleActionSuper, true);
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

        #region PrivateBattleHandling
        private void InitiateBattle(bool isBattleCanvasEnabled)
        {
            if (isBattleCanvasEnabled)
            {
                FindObjectOfType<Fader>().battleUIStateChanged -= InitiateBattle;

                if (CheckForAutoWin())
                {
                    foreach (BattleEntity enemy in activeEnemies)
                    {
                        enemy.combatParticipant.Kill();
                    }
                    SetBattleState(BattleState.Combat); // Combat will auto-complete in the usual way
                }
                else
                {
                    SetBattleState(BattleState.Intro);
                }
            }
        }

        private bool[][] GetEnemyPosition(int numberOfRows, bool[][] enemyMapping, out int rowIndex, out int columnIndex)
        {
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
                for (int i = 1; i < centerColumn; i++)
                {
                    int upperIndex = Mathf.Min(centerColumn + i, maxEnemiesPerRow - 1); // For even maxEnemiesPerRow, can overflow + 1
                    if (!enemyMapping[rowIndex][upperIndex]) { columnIndex = upperIndex; enemyMapping[rowIndex][upperIndex] = true; break; } // +1 off of center
                    if (!enemyMapping[rowIndex][centerColumn - i]) { columnIndex = centerColumn - i; enemyMapping[rowIndex][centerColumn - i] = true; break; } // -1 off of center
                }
            }
            else { enemyMapping[rowIndex][centerColumn] = true; } // Center column not populated -- default

            return enemyMapping;
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
            battleSequenceProcessed?.Invoke(battleSequence);

            // Useful Debug
            //string targetNames = string.Concat(dequeuedBattleActionData.GetTargets().Select(x => x.name));
            //UnityEngine.Debug.Log($"Battle sequence from {dequeuedBattleActionData.GetSender().name} dequeued, for action {battleSequence.battleAction.GetName()} on {targetNames}");

            // Two flags to flip to re-enable battle:
            // A) global battle queue delay, handled by coroutine
            // B) battle sequence, handled by callback (e.g. for handling effects)
            haltBattleQueue = true;
            battleSequenceInProgress = true;

            battleSequence.battleActionSuper.Use(dequeuedBattleActionData, () => { battleSequenceInProgress = false; });
            yield return new WaitForSeconds(battleQueueDelay);

            haltBattleQueue = false;
        }

        private void HandleCharacterDeath(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType != StateAlteredType.Dead) { return; }

            if (selectedCharacter == combatParticipant)
            {
                SetActiveBattleAction(null); SetSelectedCharacter(null);
                return;
            }

            if (battleActionData != null)
            {
                if (battleActionData.targetCount > 0 && battleActionData.HasTarget(combatParticipant))
                {
                    SetSelectedTarget(selectedBattleActionSuper, true);
                }
            }
        }

        private void DestroyTransientEnemies()
        {
            foreach (BattleEntity enemy in activeEnemies)
            {
                NPCStateHandler npcStateHandler = enemy.combatParticipant.GetComponent<NPCStateHandler>();
                if (npcStateHandler == null) { return; }

                if (npcStateHandler.WillDestroySelfOnDeath())
                {
                    Destroy(npcStateHandler.gameObject);
                }
            }
        }

        private void CheckForBattleEnd(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType == StateAlteredType.Dead)
            {
                bool allCharactersDead = true;
                foreach (BattleEntity battleEntity in activePlayerCharacters) { if (!battleEntity.combatParticipant.IsDead()) { allCharactersDead = false; break; } }
                if (allCharactersDead)
                {
                    SetBattleOutcome(BattleOutcome.Lost);
                    SetBattleState(BattleState.Outro);
                }

                bool allEnemiesDead = true;
                foreach (BattleEntity battleEntity in activeEnemies) { if (!battleEntity.combatParticipant.IsDead()) { allEnemiesDead = false; break; } }
                if (allEnemiesDead)
                {
                    SetBattleOutcome(BattleOutcome.Won);
                    bool isLevelUpPending = AwardExperienceToLevelUp();
                    bool isLootPending = AwardLoot();

                    if (isLevelUpPending || isLootPending)
                    {
                        SetBattleState(BattleState.PreOutro);
                    }
                    else
                    {
                        SetBattleState(BattleState.Outro);
                    }
                }
            }
        }

        private bool AwardExperienceToLevelUp()
        {
            bool levelUpTriggered = false;
            foreach (BattleEntity character in activePlayerCharacters)
            {
                Experience experience = character.combatParticipant.GetComponent<Experience>();
                if (experience == null) { continue; } // Handling for characters who do not level
                float scaledExperienceReward = 0f;

                foreach (BattleEntity enemy in GetEnemies())
                {
                    float rawExperienceReward = enemy.combatParticipant.GetExperienceReward();

                    int levelDelta = character.combatParticipant.GetLevel() - enemy.combatParticipant.GetLevel();
                    scaledExperienceReward += Experience.GetScaledExperience(rawExperienceReward, levelDelta);
                    battleExperienceReward += scaledExperienceReward;
                }

                scaledExperienceReward = Mathf.Min(scaledExperienceReward, Experience.GetMaxExperienceReward());
                if (experience.GainExperienceToLevel(scaledExperienceReward))
                {
                    levelUpTriggered = true;
                }
            }

            return levelUpTriggered;
        }

        private bool AwardLoot()
        {
            PartyKnapsackConduit partyKnapsackConduit = partyCombatConduit.GetComponent<PartyKnapsackConduit>();
            Wallet wallet = partyCombatConduit.GetComponent<Wallet>();
            if (partyKnapsackConduit == null || wallet == null) { return false; } // Failsafe, this should not happen

            bool lootAvailable = false;
            foreach (BattleEntity enemy in GetEnemies())
            {
                if (!enemy.combatParticipant.HasLoot()) { continue; }
                if (!enemy.combatParticipant.TryGetComponent(out LootDispenser lootDispenser)) { continue; }
                lootAvailable = true;

                foreach (InventoryItem inventoryItem in lootDispenser.GetItemReward())
                {
                    CombatParticipant receivingCharacter = partyKnapsackConduit.AddToFirstEmptyPartySlot(inventoryItem);
                    Tuple<string, InventoryItem> enemyItemPair = new Tuple<string, InventoryItem>(enemy.combatParticipant.GetCombatName(), inventoryItem);
                    if (receivingCharacter != null)
                    {
                        allocatedLootCart.Add(enemyItemPair);
                    }
                    else
                    {
                        unallocatedLootCart.Add(enemyItemPair);
                    }
                }

                wallet.UpdatePendingCash(lootDispenser.GetCashReward());
            }
            return lootAvailable;
        }

        private void CleanUpBattleBits()
        {
            foreach (BattleEntity character in activeCharacters)
            {
                character.combatParticipant.SetCombatActive(false);
                character.combatParticipant.stateAltered -= CheckForBattleEnd;
            }
            foreach (BattleEntity enemy in activeEnemies)
            {
                enemy.combatParticipant.SetCombatActive(false);
                enemy.combatParticipant.stateAltered -= CheckForBattleEnd;
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
            BattleController[] battleControllers = FindObjectsOfType<BattleController>();
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