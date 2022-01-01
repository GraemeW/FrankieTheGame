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
        float battleExperienceReward = 0f;

        List<CombatParticipant> activeCharacters = new List<CombatParticipant>();
        List<CombatParticipant> activeEnemies = new List<CombatParticipant>();
        CombatParticipant selectedCharacter = null;
        IBattleActionUser selectedBattleAction = null;
        bool battleActionArmed = false;
        IEnumerable<CombatParticipant> selectedTargets = null;

        Queue<BattleSequence> battleSequenceQueue = new Queue<BattleSequence>();
        bool haltBattleQueue = false;
        bool battleSequenceInProgress = false;

        List<Tuple<string,InventoryItem>> allocatedLootCart = new List<Tuple<string,InventoryItem>>();
        List<Tuple<string, InventoryItem>> unallocatedLootCart = new List<Tuple<string, InventoryItem>>();

        // Cached References
        PlayerInput playerInput = null;
        Party party = null;

        // Events
        public event Action<PlayerInputType> battleInput;
        public event Action<PlayerInputType> globalInput;
        public event Action<BattleState> battleStateChanged;
        public event Action<CombatParticipantType, IEnumerable<CombatParticipant>> selectedCombatParticipantChanged;
        public event Action<IBattleActionUser> battleActionArmedStateChanged;
        public event Action<BattleSequence> battleSequenceProcessed;

        // Interaction
        #region UnityMethods
        private void Awake()
        {
            party = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Party>();
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
                foreach (CombatParticipant character in activeCharacters)
                {
                    character.stateAltered += HandleCharacterDeath;
                }
            }
            else
            {
                foreach (CombatParticipant character in activeCharacters)
                {
                    character.stateAltered -= HandleCharacterDeath;
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

        #region PublicSettersGetters
        // Setters
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
            if (gameObject.activeSelf) { SubscribeToCharacters(true); }

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

            battleStateChanged?.Invoke(state);
        }

        public void SetBattleOutcome(BattleOutcome outcome)
        {
            this.outcome = outcome;
        }

        public bool SetSelectedCharacter(CombatParticipant character)
        {
            if (character == null) { SetActiveBattleAction(null); }
            if (character != null) { if (character.IsDead() || character.IsInCooldown()) { return false; } }

            selectedCharacter = character;
            selectedCombatParticipantChanged?.Invoke(CombatParticipantType.Character, new[] { selectedCharacter });

            return true;
        }

        public bool SetSelectedTarget(IEnumerable<CombatParticipant> targets)
        {
            selectedTargets = targets;
            selectedCombatParticipantChanged?.Invoke(CombatParticipantType.Target, selectedTargets);

            if (targets == null || targets.Count() == 0) { return false; }
            if (targets.All(x => x.IsDead())) { return false; }
            return true;
        }

        public void SetActiveBattleAction(IBattleActionUser battleAction)
        {
            if (battleAction == null)
            {
                SetBattleActionArmed(false);
                selectedCharacter?.GetComponent<SkillHandler>().ResetCurrentBranch();
                selectedBattleAction = null;
            }
            else
            {
                selectedBattleAction = battleAction;
            }
        }

        public void SetBattleActionArmed(bool enable)
        {
            if (!enable)
            {
                selectedTargets = null;
                battleActionArmed = false;
            }
            else if (selectedBattleAction != null)
            {
                SetSelectedTarget(selectedBattleAction.GetTargets(true, null, activeCharacters, activeEnemies));
                battleActionArmed = true;
            }
            else { return; }

            battleActionArmedStateChanged?.Invoke(selectedBattleAction);
        }

        // Getters
        public BattleState GetBattleState()
        {
            return state;
        }

        public BattleOutcome GetBattleOutcome()
        {
            return outcome;
        }

        public CombatParticipant GetSelectedCharacter()
        {
            AutoSelectCharacter();
            return selectedCharacter;
        }

        public IEnumerable<CombatParticipant> GetSelectedTargets()
        {
            return selectedTargets;
        }

        public IBattleActionUser GetActiveBattleAction()
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

        public bool IsBattleActionArmed()
        {
            return battleActionArmed;
        }

        public float GetBattleExperienceReward()
        {
            return battleExperienceReward;
        }

        public List<Tuple<string, InventoryItem>> GetAllocatedLootCart()
        {
            return allocatedLootCart;
        }

        public List<Tuple<string, InventoryItem>> GetUnallocatedLootCart()
        {
            return unallocatedLootCart;
        }

        public bool HasLootCart()
        {
            return HasAllocatedLootCart() || HasUnallocatedLootCart();
        }

        public bool HasAllocatedLootCart()
        {
            return allocatedLootCart != null && allocatedLootCart.Count > 0;
        }

        public bool HasUnallocatedLootCart()
        {
            return unallocatedLootCart != null && unallocatedLootCart.Count > 0;
        }

        #endregion

        #region PublicBattleHandling
        public bool AddToBattleQueue(IEnumerable<CombatParticipant> recipients)
        {
            // Called via SkillSelection UI Buttons
            // Using selected character and battle action
            if (GetSelectedCharacter() == null || selectedBattleAction == null) { return false; }

            AddToBattleQueue(GetSelectedCharacter(), recipients, selectedBattleAction);
            return true;
        }

        public void AddToBattleQueue(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, IBattleActionUser battleAction)
        {
            BattleSequence battleSequence = new BattleSequence
            {
                battleAction = battleAction,
                sender = sender,
                recipients = recipients,
            };
            AddToBattleQueue(sender, battleSequence);
        }

        private void AddToBattleQueue(CombatParticipant sender, BattleSequence battleSequence)
        {
            sender.SetCooldown(Mathf.Infinity); // Character actions locked until cooldown set by BattleController
            battleSequenceQueue.Enqueue(battleSequence);

            if (activeCharacters.Contains(sender)) { SetSelectedTarget(null); SetActiveBattleAction(null); SetSelectedCharacter(null); } // Clear out selection on player execution
        }

        public bool AttemptToRun()
        {
            float partySpeed = 0f;
            float enemySpeed = 0f;

            foreach (CombatParticipant character in activeCharacters)
            {
                partySpeed += character.GetBaseStats().GetStat(Stat.Nimble);
            }

            foreach (CombatParticipant enemy in activeEnemies)
            {
                enemySpeed += enemy.GetBaseStats().GetStat(Stat.Nimble);
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
                if (selectedBattleAction == null || selectedCharacter == null)
                {
                    SetSelectedTarget(null);
                    SetSelectedCharacter(null);
                    SetBattleState(BattleState.PreCombat); return true;
                }

                // Otherwise step out of selections
                if (selectedBattleAction != null || selectedCharacter != null)
                {
                    SetSelectedTarget(null);
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
            if (partyMemberSelect >= party.GetParty().Count) { return; } // >= since indexing off by 1 from count

            SetSelectedCharacter(party.GetParty()[partyMemberSelect]);
        }

        private bool InteractWithSkillSelect(PlayerInputType playerInputType)
        {
            if (GetBattleState() != BattleState.Combat) { return false; }
            if (IsBattleActionArmed()) { return false; }

            if (selectedCharacter != null && !selectedCharacter.IsDead())
            {
                if (playerInputType == PlayerInputType.Execute && selectedBattleAction != null)
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
            if (!IsBattleActionArmed() || selectedCharacter == null || selectedBattleAction == null) { return false; }

            if (playerInputType != PlayerInputType.DefaultNone)
            {
                if (playerInputType == PlayerInputType.Execute)
                {
                    if (selectedTargets == null || selectedTargets.Count() == 0) { return false; }
                    AddToBattleQueue(GetSelectedCharacter(), GetSelectedTargets(), selectedBattleAction);
                    SetSelectedTarget(null);
                }
                else
                {
                    if (playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
                    {
                        SetSelectedTarget(selectedBattleAction.GetTargets(true, GetSelectedTargets(), activeCharacters, activeEnemies));
                    }
                    else if (playerInputType == PlayerInputType.NavigateLeft || playerInputType == PlayerInputType.NavigateUp)
                    {
                        SetSelectedTarget(selectedBattleAction.GetTargets(false, GetSelectedTargets(), activeCharacters, activeEnemies));
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
                SetBattleState(BattleState.Intro);
            }
        }

        private void AutoSelectCharacter()
        {
            if (activeCharacters == null || selectedCharacter != null) { return; }
            CombatParticipant firstFreeCharacter = activeCharacters.Where(x => !x.IsDead() && !x.IsInCooldown()).FirstOrDefault();
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

        IEnumerator ProcessNextBattleSequence()
        {
            if (battleSequenceQueue.Count == 0) { yield break; }
            BattleSequence battleSequence = battleSequenceQueue.Dequeue();
            if (battleSequence.sender.IsDead() || battleSequence.recipients.All(x => x.IsDead())) { yield break; }
            battleSequenceProcessed?.Invoke(battleSequence);

            // Two flags to flip to re-enable battle:
            // A) global battle queue delay, handled by coroutine
            // B) battle sequence, handled by callback (e.g. for handling effects)
            haltBattleQueue = true;
            battleSequenceInProgress = true;

            battleSequence.battleAction.Use(battleSequence.sender, battleSequence.recipients, () => { battleSequenceInProgress = false; });
            yield return new WaitForSeconds(battleQueueDelay);

            haltBattleQueue = false;
        }

        private void HandleCharacterDeath(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType != StateAlteredType.Dead) { return; }

            if (selectedCharacter == combatParticipant)
            {
                SetSelectedTarget(null); SetActiveBattleAction(null); SetSelectedCharacter(null);
                return;
            }

            if (selectedTargets != null && selectedTargets.Contains(combatParticipant))
            {
                SetSelectedTarget(null);
            }
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
            foreach (CombatParticipant character in GetCharacters())
            {
                Experience experience = character.GetComponent<Experience>();
                if (experience == null) { continue; } // Handling for characters who do not level
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
                    levelUpTriggered = true;
                }
            }

            return levelUpTriggered;
        }

        private bool AwardLoot()
        {
            PartyKnapsackConduit partyKnapsackConduit = party.GetComponent<PartyKnapsackConduit>();
            Wallet wallet = party.GetComponent<Wallet>();
            if (partyKnapsackConduit == null || wallet == null) { return false; } // Failsafe, this should not happen

            bool lootAvailable = false;
            foreach (CombatParticipant enemy in GetEnemies())
            {
                if (!enemy.HasLoot()) { continue; }
                if (!enemy.TryGetComponent(out LootDispenser lootDispenser)) { continue; }
                lootAvailable = true;

                foreach (InventoryItem inventoryItem in lootDispenser.GetItemReward())
                {
                    CombatParticipant receivingCharacter = partyKnapsackConduit.AddToFirstEmptyPartySlot(inventoryItem);
                    Tuple<string, InventoryItem> enemyItemPair = new Tuple<string, InventoryItem>(enemy.GetCombatName(), inventoryItem);
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