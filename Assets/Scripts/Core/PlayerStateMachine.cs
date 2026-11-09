using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LowDefMustard.Core;
using LowDefMustard.Zones;
using LowDefMustard.Utils;
using Frankie.Core.PlayerStates;
using Frankie.Core.PlayerStateMemory;
using Frankie.Control;
using Frankie.Combat;
using Frankie.Speech;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.World;
using Frankie.Zones;
using Unity.Scripting.LifecycleManagement;

namespace Frankie.Core
{
    [RequireComponent(typeof(Party))]
    [RequireComponent(typeof(PartyAssist))]
    [RequireComponent(typeof(PartyCombatConduit))]
    [RequireComponent(typeof(Shopper))]
    public class PlayerStateMachine : MonoBehaviour, IPlayerStateContext
    {
        // Tunables
        [Header("Other Controller Prefabs")]
        [SerializeField] private BattleController battleControllerPrefab;
        [SerializeField] private DialogueController dialogueControllerPrefab;
        [SerializeField] private GameObject battleUIPrefab;
        [Header("Menu Game Objects")]
        [SerializeField] private GameObject shopSelectPrefab;
        [SerializeField] private GameObject cashTransferPrefab;
        [SerializeField] private GameObject worldOptionsPrefab;
        [SerializeField] private GameObject escapeMenuPrefab;
        [Header("Parameters")]
        [SerializeField] private int maxEnemiesPerCombat = 21;
        [Tooltip("seconds, incl. battle fade-out time")][SerializeField] private float immunityTimePostCombat = 3.5f;
        
        // Const
        private const int _queuePopFrameSkips = 1;

        // State Information
        // Player
        private IPlayerState currentPlayerState = new WorldState();
        private readonly PlayerStateRequestDispatcher requestDispatcher = new();
        private readonly ActionMemory actionMemory = new();
        
        private TransitionMemory transitionMemory = new();
        private CutsceneMemory cutsceneMemory = new();
        private CombatMemory combatMemory = new();
        private DialogueMemory dialogueMemory = new();
        private TradeMemory tradeMemory = new();
        private OptionMemory optionMemory = new();
        
        // Coroutines
        private Coroutine queuePopCoroutine;
        private Coroutine immunityCoroutine;

        // Cached References -- Persistent
        private Party party;
        private PartyAssist partyAssist;
        private PartyCombatConduit partyCombatConduit;
        private Shopper shopper;
        // Cached References -- State Dependent
        private WorldUIRoot worldUIRoot;
        private BattleController battleController;
        private DialogueController dialogueController;

        // Events
        public event Action<PlayerStateType, IPlayerStateInfo> playerStateChanged;
        public event Action<int, int, bool> playerLayerChanged;

        #region Static
        [NoAutoStaticsCleanup] public static readonly TransitionState transitionState = new();
        [NoAutoStaticsCleanup] public static readonly CombatState combatState = new();
        [NoAutoStaticsCleanup] public static readonly DialogueState dialogueState = new();
        [NoAutoStaticsCleanup] public static readonly TradeState tradeState = new();
        [NoAutoStaticsCleanup] public static readonly OptionState optionState = new();
        [NoAutoStaticsCleanup] public static readonly CutSceneState cutSceneState = new();
        [NoAutoStaticsCleanup] public static readonly WorldState worldState = new();
        #endregion

        #region UnityStandardMethods
        private void Awake()
        {
            party = GetComponent<Party>();
            partyAssist = GetComponent<PartyAssist>();
            partyCombatConduit = GetComponent<PartyCombatConduit>();
            shopper = GetComponent<Shopper>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += UpdateReferencesForNewScene;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= UpdateReferencesForNewScene;
        }

        private void OnDestroy()
        {
            if (queuePopCoroutine != null) { StopCoroutine(queuePopCoroutine); }
            if (immunityCoroutine != null) { StopCoroutine(immunityCoroutine); }
        }

        private void Update()
        {
            if (!actionMemory.ReadyToPopQueue()) { return; }
            
            if (queuePopCoroutine != null) { StopCoroutine(queuePopCoroutine); }
            queuePopCoroutine = StartCoroutine(actionMemory.TryPopQueue(_queuePopFrameSkips));
        }
        #endregion

        #region SettersGetters
        void IPlayerStateContext.SetPlayerState(IPlayerState playerState)
        {
            PlayerStateType playerStateType = playerState.playerStateType;
            LDMLog.Info(LDMLogCategory.Core, $"Player state set to '{playerStateType}'.", this);

            currentPlayerState = playerState;
            playerStateChanged?.Invoke(playerStateType, this);
            
            // Pop on update to prevent same-frame multi-state change (otherwise can experience bugs with controller spawning while deconstructing conflicting w/ singleton logic)
            actionMemory.SetReadyToPop(playerStateType);

            // Exception:  allow swarm / multi-battle entry on same-frame ~ runs inline (synchronous fader completion would otherwise race the chain)
            if (playerStateType == PlayerStateType.InTransition && transitionMemory.InBattleEntryTransition()) { requestDispatcher.RunInline(() => actionMemory.ChainQueuedCombatAction()); }
        }
        
        public PlayerStateType GetPlayerStateType() => currentPlayerState.playerStateType;
        public Party GetParty() => party;
        bool IPlayerStateInfo.CanMoveInCutscene() => cutsceneMemory.canMoveInCutscene;
        
        public void SetPostDialogueCallbackActions(InteractionEvent interactionEvent)
        {
            if (interactionEvent == null) { return; }
            if (dialogueController == null)
            {
                LDMLog.Warning(LDMLogCategory.Core, "No dialogue controller to attach post-dialogue callback actions to. Dropping them.", this);
                return;
            }
            dialogueController.SetDestroyCallbackActions(interactionEvent);
        }
        
        private void UpdateReferencesForNewScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            worldUIRoot = WorldUIRoot.FindWorldUIRoot();
        }
        private WorldUIRoot GetWorldUIRoot() // Fallback for a player spawned after its scene loaded (sceneLoaded already raised)
        {
            if (worldUIRoot == null) { worldUIRoot = WorldUIRoot.FindWorldUIRoot(); }
            return worldUIRoot;
        }
        
        private void SetPlayerImmunity(bool enablePlayerImmunity)
        {
            int standardLayer = enablePlayerImmunity ? Player.GetImmunePlayerLayer() :  Player.GetPlayerLayer();
            int probeLayer = enablePlayerImmunity ? Player.GetImmunePlayerLayer() : Player.GetPlayerInteractionProbeLayer();
            gameObject.layer = standardLayer;
            
            // Party member layer updates via event -> Party
            playerLayerChanged?.Invoke(standardLayer, probeLayer, enablePlayerImmunity);
        }
        #endregion

        #region StateTransitions
        public void EnterWorld()
        {
            requestDispatcher.Run(nameof(EnterWorld), () => currentPlayerState.EnterWorld(this));
        }

        public void EnterZoneTransition()
        {
            requestDispatcher.Run(nameof(EnterZoneTransition), () =>
            {
                // Do not carry queued actions across zones
                actionMemory.ClearQueuedActions();
                actionMemory.SetActionUnderConsideration(PlayerStateType.InTransition, EnterZoneTransition);
                transitionMemory.currentTransitionType = TransitionType.Zone;
                transitionMemory.zoneTransitionComplete = false;
                
                currentPlayerState.EnterTransition(this);
            });
        }

        public void EnterCombat(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            if (enemies == null || enemies.Count == 0 || !TransitionMemory.IsBattleTransition(transitionType)) { return; }

            requestDispatcher.Run(nameof(EnterCombat), () =>
            {
                actionMemory.SetActionUnderConsideration(PlayerStateType.InBattle, () => EnterCombat(enemies, transitionType));
                combatMemory.enemiesUnderConsideration.Clear();
                combatMemory.enemiesUnderConsideration.AddRange(enemies);
                transitionMemory.transitionTypeUnderConsideration = transitionType;

                currentPlayerState.EnterCombat(this);
            });
        }

        public void EnterDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            if (newConversant == null || newDialogue == null) { return; }

            requestDispatcher.Run(nameof(EnterDialogue), () =>
            {
                actionMemory.SetActionUnderConsideration(PlayerStateType.InDialogue, () => EnterDialogue(newConversant, newDialogue));
                dialogueMemory.dialogueData = new DialogueData(newConversant, newDialogue);
                currentPlayerState.EnterDialogue(this);
            });
        }

        public void EnterDialogue(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }

            requestDispatcher.Run(nameof(EnterDialogue), () =>
            {
                actionMemory.SetActionUnderConsideration(PlayerStateType.InDialogue, () => EnterDialogue(message));
                dialogueMemory.dialogueData = new DialogueData(message);
                currentPlayerState.EnterDialogue(this);
            });
        }

        public void EnterDialogue(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            if (choiceActionPairs == null || choiceActionPairs.Count == 0) { return; }

            requestDispatcher.Run(nameof(EnterDialogue), () =>
            {
                actionMemory.SetActionUnderConsideration(PlayerStateType.InDialogue, () => EnterDialogue(message, choiceActionPairs));
                dialogueMemory.dialogueData = new DialogueData(message, choiceActionPairs);
                currentPlayerState.EnterDialogue(this);
            });
        }

        public void EnterShop(Shop shop)
        {
            if (shopper == null || shop == null) { return; }

            requestDispatcher.Run(nameof(EnterShop), () =>
            {
                actionMemory.SetActionUnderConsideration(PlayerStateType.InMenus, () => EnterShop(shop));
                shopper.SetShop(shop);
                tradeMemory.tradeData = new TradeData(shop.GetShopType());
                currentPlayerState.EnterTrade(this);
            });
        }

        public void EnterBank(BankType bankType)
        {
            if (bankType == BankType.None) { return; }

            requestDispatcher.Run(nameof(EnterBank), () =>
            {
                actionMemory.SetActionUnderConsideration(PlayerStateType.InMenus, () => EnterBank(bankType));
                shopper.SetBankType(bankType);
                tradeMemory.tradeData = new TradeData(bankType);
                currentPlayerState.EnterTrade(this);
            });
        }

        public void EnterWorldOptions()
        {
            requestDispatcher.Run(nameof(EnterWorldOptions), () =>
            {
                optionMemory.optionStateType = OptionStateType.WorldOptions;
                currentPlayerState.EnterOptions(this);
            });
        }

        public void EnterEscapeMenu()
        {
            requestDispatcher.Run(nameof(EnterEscapeMenu), () =>
            {
                optionMemory.optionStateType = OptionStateType.EscapeMenu;
                currentPlayerState.EnterOptions(this);
            });
        }

        public void EnterCutscene(bool playerVisible = true, bool canMove = false)
        {
            requestDispatcher.Run(nameof(EnterCutscene), () =>
            {
                actionMemory.SetActionUnderConsideration(PlayerStateType.InCutScene, () => EnterCutscene(playerVisible, canMove));
                cutsceneMemory.visibleDuringCutscene = playerVisible;
                cutsceneMemory.canMoveInCutscene = canMove && playerVisible;
                currentPlayerState.EnterCutScene(this);
            });
        }
        #endregion

        #region UtilityTransition
        public void SetZoneTransitionStatus(bool complete) => transitionMemory.zoneTransitionComplete = complete;
        bool IPlayerStateInfo.InZoneTransition() => transitionMemory.currentTransitionType == TransitionType.Zone;
        bool ITransitionStateContext.IsZoneTransitionComplete() => transitionMemory.zoneTransitionComplete;
        void ITransitionStateContext.ConfirmTransitionType() => transitionMemory.ConfirmTransitionType();
        bool ITransitionStateContext.InBattleEntryTransition() => transitionMemory.InBattleEntryTransition();
        bool IPlayerStateInfo.InBattleExitTransition() => transitionMemory.InBattleExitTransition();
        #endregion

        #region UtilityCombat
        bool ICombatStateContext.IsCombatFadeComplete() => combatMemory.combatFadeComplete;
        bool IPlayerStateInfo.IsAnyPartyMemberAlive() => partyCombatConduit.IsAnyMemberAlive();
        bool IPlayerStateInfo.IsPlayerFearsome(CombatParticipant combatParticipant) => partyCombatConduit.IsFearsome(combatParticipant);
        bool ICombatStateContext.AreCombatParticipantsValid() => combatMemory.AreCombatParticipantsValid();
        void ICombatStateContext.ConfirmEnemiesUnderConsideration() => combatMemory.ShiftEnemiesFromConsiderationToTransition(maxEnemiesPerCombat);
        void ICombatStateContext.SetupBattleController()
        {
            if (battleController == null)
            {
                BattleController existingBattleController = BattleController.FindBattleController();
                battleController = existingBattleController == null ? Instantiate(battleControllerPrefab) : existingBattleController;
            }
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleCombatMessages);
        }

        bool ICombatStateContext.StartBattleSequence()
        {
            // Edge case on improper game exit, starting Coroutine on object if it's undergoing destruction throws error
            if (this == null || gameObject == null) { return false; }
            
            TransitionType currentTransitionType = transitionMemory.currentTransitionType;
            var faderEventTriggers = new FaderEventTriggers<TransitionType>(null, () => OnBattleEntryPeak(currentTransitionType), null, () => OnBattleEntryComplete(currentTransitionType));
            return combatMemory.BeginFade(currentTransitionType, faderEventTriggers);
        }

        bool ICombatStateContext.EndBattleSequence()
        {
            // Edge case on improper game exit, starting Coroutine on object if it's undergoing destruction throws error
            if (this == null || gameObject == null) { return false; }

            transitionMemory.currentTransitionType = TransitionType.BattleComplete;
            var faderEventTriggers = new FaderEventTriggers<TransitionType>(null, OnBattleExitPeak, null, OnBattleExitComplete);
            return combatMemory.ConcludeFade(TransitionType.BattleComplete, faderEventTriggers);
        }

        void ICombatStateContext.AbortBattle()
        {
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleCombatMessages); // else the abort's Complete (outcome Aborted) reads as a true battle exit
            if (battleController == null) { battleController = BattleController.FindBattleController(); }
            if (battleController != null) { battleController.AbortBattle(); }

            // Hold battle UI on screen until e.g. zone fade covers it
            BattleController abortedBattleController = battleController;
            GameObject abortedBattleUI = combatMemory.battleUIInstance;
            battleController = null;
            combatMemory = new CombatMemory();
            if (!Fader.TryQueueAtFadePeak(() => RetireAbortedBattle(abortedBattleController, abortedBattleUI))) { RetireAbortedBattle(abortedBattleController, abortedBattleUI); }
        }

        private static void RetireAbortedBattle(BattleController abortedBattleController, GameObject abortedBattleUI)
        {
            if (abortedBattleController != null) { abortedBattleController.Retire(); }
            if (abortedBattleUI != null) { Destroy(abortedBattleUI); }
        }

        private void HandleCombatMessages(BattleStateChangedEvent battleStateChangedEvent)
        {
            BattleState battleState = battleStateChangedEvent.battleState;
            if (battleState != BattleState.Complete) { return; }
            
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleCombatMessages);
            requestDispatcher.Run(nameof(HandleCombatMessages), () =>
            {
                transitionMemory.currentTransitionType = TransitionType.BattleComplete;
                currentPlayerState.EnterTransition(this);
            });
        }
        
        private void OnBattleEntryPeak(TransitionType transitionType)
        {
            if (battleUIPrefab != null) { combatMemory.battleUIInstance = Instantiate(battleUIPrefab); }
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.EntryPeak, combatMemory.enemiesInTransition, transitionType));
        }

        private void OnBattleEntryComplete(TransitionType transitionType)
        {
            requestDispatcher.Run(nameof(OnBattleEntryComplete), () =>
            {
                combatMemory.combatFadeComplete = true;
                currentPlayerState.EnterCombat(this);
                BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.EntryComplete, combatMemory.enemiesInTransition, transitionType));
            });
        }

        private void OnBattleExitPeak()
        {
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.ExitPeak));
            if (immunityCoroutine != null) { StopCoroutine(immunityCoroutine); }
            immunityCoroutine = StartCoroutine(TimedCollisionDisable());
        }

        private void OnBattleExitComplete()
        {
            requestDispatcher.Run(nameof(OnBattleExitComplete), () =>
            {
                BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.ExitComplete));
                if (combatMemory.battleUIInstance != null) { Destroy(combatMemory.battleUIInstance); }
                currentPlayerState.EnterWorld(this);
            });
        }
        #endregion

        #region UtilityDialogue
        bool IDialogueStateContext.SetupDialogueController()
        {
            if (dialogueController == null)
            {
                DialogueController existingDialogueController = DialogueController.FindDialogueController();
                dialogueController = existingDialogueController == null ? Instantiate(dialogueControllerPrefab) : existingDialogueController;
            }
            
            if (dialogueController.IsClosing()) // A closing controller can't be reused (its OnDestroy callbacks are pending) + a new one would fail VerifyUnique this frame
            {
                dialogueController = null;
                LDMLog.Info(LDMLogCategory.Core, "Dialogue controller is closing. Retrying the dialogue request next frame.", this);
                return false;
            }
            
            dialogueController.Setup(GetWorldUIRoot(), this, party);
            return true;
        }

        bool IDialogueStateContext.StartDialogueSequence() => dialogueMemory.InitiateDialogue(dialogueController);
        #endregion

        #region UtilityTrade
        bool ITradeStateContext.StartTradeSequence()
        {
            return tradeMemory.InitiateTrade(
                shopper.GetCurrentShop(),
                () => Instantiate(shopSelectPrefab, GetWorldUIRoot().transform),
                () => Instantiate(cashTransferPrefab, GetWorldUIRoot().transform));
        }
        #endregion

        #region UtilityOption
        bool IOptionStateContext.StartOptionSequence()
        {
            return optionMemory.InitiateOptions(
                () => Instantiate(worldOptionsPrefab, GetWorldUIRoot().transform), 
                () => Instantiate(escapeMenuPrefab, GetWorldUIRoot().transform));
        }
        #endregion

        #region UtilityGeneral
        void IPlayerStateContext.TogglePlayerVisibility(bool? enable)
        {
            bool visible = enable ?? cutsceneMemory.visibleDuringCutscene;
            if (party != null) { party.TogglePartyVisible(visible); }
            if (partyAssist != null) { partyAssist.TogglePartyVisible(visible); }
        }
        
        private IEnumerator TimedCollisionDisable()
        {
            SetPlayerImmunity(true);
            yield return new WaitForSeconds(immunityTimePostCombat);
            SetPlayerImmunity(false);
            immunityCoroutine = null;
        }

        void IPlayerStateContext.QueueActionUnderConsideration() => actionMemory.QueueActionUnderConsideration();
        void IPlayerStateContext.RequestQueuePop() => actionMemory.SetReadyToPop(currentPlayerState.playerStateType);

        void IPlayerStateContext.ClearPlayerStateMemory()
        {
            battleController = null;
            dialogueController = null;
            if (shopper != null)
            {
                shopper.SetShop(null);
                shopper.SetBankType(BankType.None);
            }
            
            // Note:  We do NOT call a new ActionMemory here, as QueuedActions must persist
            actionMemory.ResetActionUnderConsideration();
            
            transitionMemory = new TransitionMemory();
            cutsceneMemory = new CutsceneMemory();
            combatMemory = new CombatMemory();
            dialogueMemory = new DialogueMemory();
            tradeMemory = new TradeMemory();
            optionMemory = new OptionMemory();
        }
        #endregion
    }
}
