using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Frankie.Core.PlayerStates;
using Frankie.Core.PlayerStateMemory;
using Frankie.Control;
using Frankie.Combat;
using Frankie.Speech;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.World;
using Frankie.ZoneManagement;
using Frankie.Utils;

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
        [SerializeField] private int maxEnemiesPerCombat = 12;
        [Tooltip("seconds, incl. battle fade-out time")][SerializeField] private float immunityTimePostCombat = 3.5f;
        
        // Const
        private int queuePopFrameSkips = 1;
        
        // State Information
        // Player
        private IPlayerState currentPlayerState = new WorldState();
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
        private WorldCanvas worldCanvas;
        private BattleController battleController;
        private DialogueController dialogueController;

        // Events
        public event Action<PlayerStateType, IPlayerStateContext> playerStateChanged;
        public event Action<int, int, bool> playerLayerChanged;

        #region Static
        public static readonly TransitionState transitionState = new();
        public static readonly CombatState combatState = new();
        public static readonly DialogueState dialogueState = new();
        public static readonly TradeState tradeState = new();
        public static readonly OptionState optionState = new();
        public static readonly CutSceneState cutSceneState = new();
        public static readonly WorldState worldState = new();
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
            queuePopCoroutine = StartCoroutine(actionMemory.TryPopQueue(queuePopFrameSkips));
        }
        #endregion

        #region SettersGetters
        void IPlayerStateContext.SetPlayerState(IPlayerState playerState)
        {
            PlayerStateType playerStateType = playerState.playerStateType;
            Debug.Log($"Updating player state to: {Enum.GetName(typeof(PlayerStateType), playerStateType)}");

            currentPlayerState = playerState;
            playerStateChanged?.Invoke(playerStateType, this);
            actionMemory.SetReadyToPop(playerStateType);
            
            // Pop on update to prevent same-frame multi-state change
            // Otherwise can experience bugs with controller spawning while deconstructing conflicting w/ singleton logic

            // Allow swarm / multi-battle entry on same-frame
            if (playerStateType == PlayerStateType.InTransition && InBattleEntryTransition()) { actionMemory.ChainQueuedCombatAction(); }
        }
        
        public Party GetParty() => party;
        public bool CanMoveInCutscene() => cutsceneMemory.canMoveInCutscene;
        
        public void SetPostDialogueCallbackActions(InteractionEvent interactionEvent)
        {
            if (dialogueController == null || interactionEvent == null) { return; }
            dialogueController.SetDestroyCallbackActions(interactionEvent);
        }
        
        private void UpdateReferencesForNewScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
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
            currentPlayerState.EnterWorld(this);
        }

        public void EnterZoneTransition()
        {
            // Do not carry queued actions across zones
            actionMemory.ClearQueuedActions();
            actionMemory.SetActionUnderConsideration(PlayerStateType.InTransition, EnterZoneTransition);
            transitionMemory.currentTransitionType = TransitionType.Zone;
            transitionMemory.zoneTransitionComplete = false;
            
            currentPlayerState.EnterTransition(this);
        }

        public void EnterCombat(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            if (enemies == null || enemies.Count == 0 || !TransitionMemory.IsBattleTransition(transitionType)) { return; }

            actionMemory.SetActionUnderConsideration(PlayerStateType.InBattle, () => EnterCombat(enemies, transitionType));
            combatMemory.enemiesUnderConsideration.Clear();
            combatMemory.enemiesUnderConsideration.AddRange(enemies);
            transitionMemory.transitionTypeUnderConsideration = transitionType;

            currentPlayerState.EnterCombat(this);
        }

        public void EnterDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            if (newConversant == null || newDialogue == null) { return; }

            actionMemory.SetActionUnderConsideration(PlayerStateType.InDialogue, () => EnterDialogue(newConversant, newDialogue));
            dialogueMemory.dialogueData = new DialogueData(newConversant, newDialogue);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterDialogue(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }

            actionMemory.SetActionUnderConsideration(PlayerStateType.InDialogue, () => EnterDialogue(message));
            dialogueMemory.dialogueData = new DialogueData(message);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterDialogue(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            if (choiceActionPairs == null || choiceActionPairs.Count == 0) { return; }

            actionMemory.SetActionUnderConsideration(PlayerStateType.InDialogue, () => EnterDialogue(message, choiceActionPairs));
            dialogueMemory.dialogueData = new DialogueData(message, choiceActionPairs);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterShop(Shop shop)
        {
            if (shopper == null || shop == null) { return; }

            actionMemory.SetActionUnderConsideration(PlayerStateType.InMenus, () => EnterShop(shop));
            shopper.SetShop(shop);
            tradeMemory.tradeData = new TradeData(shop.GetShopType());
            currentPlayerState.EnterTrade(this);
        }

        public void EnterBank(BankType bankType)
        {
            if (bankType == BankType.None) { return; }

            actionMemory.SetActionUnderConsideration(PlayerStateType.InMenus, () => EnterBank(bankType));
            shopper.SetBankType(bankType);
            tradeMemory.tradeData = new TradeData(bankType);
            currentPlayerState.EnterTrade(this);
        }

        public void EnterWorldOptions()
        {
            optionMemory.optionStateType = OptionStateType.WorldOptions;
            currentPlayerState.EnterOptions(this);
        }

        public void EnterEscapeMenu()
        {
            optionMemory.optionStateType = OptionStateType.EscapeMenu;
            currentPlayerState.EnterOptions(this);
        }

        public void EnterCutscene(bool playerVisible = true, bool canMove = false)
        {
            actionMemory.SetActionUnderConsideration(PlayerStateType.InCutScene, () => EnterCutscene(playerVisible));
            cutsceneMemory.visibleDuringCutscene = playerVisible;
            cutsceneMemory.canMoveInCutscene = canMove && playerVisible;
            currentPlayerState.EnterCutScene(this);
        }
        #endregion

        #region UtilityTransition
        
        public bool InZoneTransition() => transitionMemory.currentTransitionType == TransitionType.Zone;
        public bool IsZoneTransitionComplete() => transitionMemory.zoneTransitionComplete;
        public void SetZoneTransitionStatus(bool complete) => transitionMemory.zoneTransitionComplete = complete;
        public void ConfirmTransitionType() => transitionMemory.ConfirmTransitionType();
        public bool InBattleEntryTransition() => transitionMemory.InBattleEntryTransition();
        public bool InBattleExitTransition() => transitionMemory.InBattleExitTransition();
        #endregion

        #region UtilityCombat
        public bool IsCombatFadeComplete() => combatMemory.combatFadeComplete;
        public bool IsAnyPartyMemberAlive() => partyCombatConduit.IsAnyMemberAlive();
        public bool IsPlayerFearsome(CombatParticipant combatParticipant) => partyCombatConduit.IsFearsome(combatParticipant);
        public bool AreCombatParticipantsValid() => combatMemory.AreCombatParticipantsValid();
        public void ConfirmEnemiesUnderConsideration() => combatMemory.ShiftEnemiesFromConsiderationToTransition(maxEnemiesPerCombat);
        public void SetupBattleController()
        {
            if (battleController == null)
            {
                BattleController existingBattleController = BattleController.FindBattleController();
                battleController = existingBattleController == null ? Instantiate(battleControllerPrefab) : existingBattleController;
            }
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleCombatMessages);
        }

        public bool StartBattleSequence()
        {
            // Edge case on improper game exit, starting Coroutine on object if it's undergoing destruction throws error
            if (this == null || gameObject == null) { return false; }
            
            TransitionType currentTransitionType = transitionMemory.currentTransitionType;
            var faderEventTriggers = new FaderEventTriggers(null, () => OnBattleEntryPeak(currentTransitionType), null, () => OnBattleEntryComplete(currentTransitionType));
            return combatMemory.BeginFade(currentTransitionType, faderEventTriggers);
        }

        public bool EndBattleSequence()
        {
            // Edge case on improper game exit, starting Coroutine on object if it's undergoing destruction throws error
            if (this == null || gameObject == null) { return false; }

            transitionMemory.currentTransitionType = TransitionType.BattleComplete;
            var faderEventTriggers = new FaderEventTriggers(null, OnBattleExitPeak, null, OnBattleExitComplete);
            return combatMemory.ConcludeFade(TransitionType.BattleComplete, faderEventTriggers);
        }

        private void HandleCombatMessages(BattleStateChangedEvent battleStateChangedEvent)
        {
            BattleState battleState = battleStateChangedEvent.battleState;
            if (battleState != BattleState.Complete) { return; }
            
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleCombatMessages);
            transitionMemory.currentTransitionType = TransitionType.BattleComplete;
            currentPlayerState.EnterTransition(this);
        }
        
        private void OnBattleEntryPeak(TransitionType transitionType)
        {
            if (battleUIPrefab != null) { Instantiate(battleUIPrefab); }
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.EntryPeak, combatMemory.enemiesInTransition, transitionType));
        }

        private void OnBattleEntryComplete(TransitionType transitionType)
        {
            combatMemory.combatFadeComplete = true;
            currentPlayerState.EnterCombat(this);
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.EntryComplete, combatMemory.enemiesInTransition, transitionType));
        }

        private void OnBattleExitPeak()
        {
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.ExitPeak));
            if (immunityCoroutine != null) { StopCoroutine(immunityCoroutine); }
            immunityCoroutine = StartCoroutine(TimedCollisionDisable());
        }

        private void OnBattleExitComplete()
        {
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.ExitComplete));
            currentPlayerState.EnterWorld(this);
        }
        #endregion

        #region UtilityDialogue
        public void SetupDialogueController()
        {
            if (dialogueController == null)
            {
                DialogueController existingDialogueController = DialogueController.FindDialogueController();
                dialogueController = existingDialogueController == null ? Instantiate(dialogueControllerPrefab) : existingDialogueController;
            }
            dialogueController.Setup(worldCanvas, this, party);
        }

        public bool StartDialogueSequence() => dialogueMemory.InitiateDialogue(dialogueController);
        #endregion

        #region UtilityTrade
        public bool StartTradeSequence()
        {
            return tradeMemory.InitiateTrade(
                () => Instantiate(shopSelectPrefab, worldCanvas.gameObject.transform),
                () => Instantiate(cashTransferPrefab, worldCanvas.gameObject.transform));
        }
        #endregion

        #region UtilityOption
        public bool StartOptionSequence()
        {
            return optionMemory.InitiateOptions(
                () => Instantiate(worldOptionsPrefab, worldCanvas.gameObject.transform), 
                () => Instantiate(escapeMenuPrefab, worldCanvas.gameObject.transform));
        }
        #endregion

        #region UtilityGeneral
        public void TogglePlayerVisibility(bool? enable = null)
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

        public void QueueActionUnderConsideration() => actionMemory.QueueActionUnderConsideration();

        public void ClearPlayerStateMemory()
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
