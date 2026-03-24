using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Frankie.Control.PlayerStates;
using Frankie.Combat;
using Frankie.Core;
using Frankie.ZoneManagement;
using Frankie.Speech;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.World;
using Frankie.Utils;

namespace Frankie.Control
{
    [RequireComponent(typeof(Party))]
    [RequireComponent(typeof(PartyAssist))]
    [RequireComponent(typeof(PartyCombatConduit))]
    [RequireComponent(typeof(Shopper))]
    public class PlayerStateMachine : MonoBehaviour, IPlayerStateContext
    {
        // Tunables
        [Header("Hookups")] 
        [SerializeField] private GameObject interactionCenterPoint;
        [Header("Other Controller Prefabs")]
        [SerializeField] private BattleController battleControllerPrefab;
        [SerializeField] private DialogueController dialogueControllerPrefab;
        [SerializeField] private GameObject battleUIPrefab;
        [Header("Menu Game Objects")]
        [SerializeField] private GameObject shopSelectPrefab;
        [SerializeField] private GameObject cashTransferPrefab;
        [SerializeField] private GameObject worldOptionsPrefab;
        [SerializeField] private GameObject escapeMenuPrefab;
        [Header("Messages")]
        [SerializeField] private string messageCannotFight = "You are wounded and cannot fight.";
        [Header("Parameters")]
        [SerializeField] private int maxEnemiesPerCombat = 12;
        [Tooltip("seconds, incl. battle fade-out time")][SerializeField] private float immunityTimePostCombat = 3.5f;

        // State Information
        // Player
        private IPlayerState currentPlayerState = new WorldState();
        // Queue
        private PlayerStateTypeActionPair actionUnderConsideration;
        private readonly Stack<PlayerStateTypeActionPair> queuedActions = new();
        private bool readyToPopQueue = false;
        // Transition
        private TransitionType transitionTypeUnderConsideration = TransitionType.None;
        private TransitionType currentTransitionType = TransitionType.None;
        private bool zoneTransitionComplete = true;

        // CutScene
        private bool visibleDuringCutscene = true;
        private bool canMoveInCutscene = false;
        // Combat
        private bool combatFadeComplete = false;
        private readonly List<CombatParticipant> enemiesUnderConsideration = new();
        private readonly List<CombatParticipant> enemiesInTransition = new();
        // Dialogue
        private DialogueData dialogueData;
        // Trade
        private TradeData tradeData;
        // Option
        private OptionStateType optionStateType;

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
        public event Action<int, bool> playerLayerChanged;

        // Data Structures
        private class PlayerStateTypeActionPair
        {
            public PlayerStateTypeActionPair(PlayerStateType playerStateType, Action action)
            {
                this.playerStateType = playerStateType;
                this.action = action;
            }

            public PlayerStateType playerStateType { get; }
            public Action action { get; }
        }

        #region StaticMethods
        private static PlayerStateType TranslatePlayerState(IPlayerState playerState)
        {
            Type playerStateType = playerState.GetType();
            if (playerStateType == typeof(TransitionState)) { return PlayerStateType.InTransition; }
            if (playerStateType == typeof(CombatState)) { return PlayerStateType.InBattle; }
            if (playerStateType == typeof(DialogueState)) { return PlayerStateType.InDialogue; }
            if (playerStateType == typeof(TradeState) || playerStateType == typeof(OptionState)) { return PlayerStateType.InMenus; }
            if (playerStateType == typeof(CutSceneState)) { return PlayerStateType.InCutScene; }
            return PlayerStateType.InWorld; // Default:  typeof(WorldState)
        }
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

        private void Update()
        {
            if (readyToPopQueue)
            {
                readyToPopQueue = false; // Either popped queue will change state, or queue invalidated -- clear state
                PopQueuedAction();
            }
        }
        #endregion

        #region SettersGetters
        void IPlayerStateContext.SetPlayerState(IPlayerState playerState)
        {
            PlayerStateType playerStateType = TranslatePlayerState(playerState);
            Debug.Log($"Updating player state to: {Enum.GetName(typeof(PlayerStateType), playerStateType)}");

            currentPlayerState = playerState;
            
            
            playerStateChanged?.Invoke(playerStateType, this);

            readyToPopQueue = playerStateType == PlayerStateType.InWorld;
            // Pop on update to prevent same-frame multi-state change
            // Otherwise can experience bugs with controller spawning while deconstructing conflicting w/ singleton logic

            if (playerStateType == PlayerStateType.InTransition && InBattleEntryTransition()) { ChainQueuedCombatAction(); }
            // Required to allow swarm / multi-battle entry on same-frame
        }
        
        public Party GetParty() => party;
        public bool CanMoveInCutscene() => canMoveInCutscene;
        
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
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = standardLayer;
            }
            interactionCenterPoint.layer = probeLayer;
            
            playerLayerChanged?.Invoke(standardLayer, enablePlayerImmunity);
        }
        #endregion

        #region StateTransitions
        public void EnterWorld()
        {
            currentPlayerState.EnterWorld(this);
        }

        public void EnterZoneTransition()
        {
            queuedActions.Clear(); // Do not carry queued actions across zones
            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.InTransition, EnterZoneTransition);
            currentTransitionType = TransitionType.Zone;
            zoneTransitionComplete = false;
            currentPlayerState.EnterTransition(this);
        }

        public void EnterCombat(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            if (enemies == null || enemies.Count == 0 || !IsBattleTransition(transitionType)) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.InBattle, () => EnterCombat(enemies, transitionType));
            enemiesUnderConsideration.Clear();
            enemiesUnderConsideration.AddRange(enemies);
            transitionTypeUnderConsideration = transitionType;

            currentPlayerState.EnterCombat(this);
        }

        public void EnterDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            if (newConversant == null || newDialogue == null) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.InDialogue, () => EnterDialogue(newConversant, newDialogue));
            dialogueData = new DialogueData(newConversant, newDialogue);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterDialogue(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.InDialogue, () => EnterDialogue(message));
            dialogueData = new DialogueData(message);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterDialogue(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            if (choiceActionPairs == null || choiceActionPairs.Count == 0) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.InDialogue, () => EnterDialogue(message, choiceActionPairs));
            dialogueData = new DialogueData(message, choiceActionPairs);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterShop(Shop shop)
        {
            if (shopper == null || shop == null) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.InMenus, () => EnterShop(shop));
            shopper.SetShop(shop);
            tradeData = new TradeData(shop.GetShopType());
            currentPlayerState.EnterTrade(this);
        }

        public void EnterBank(BankType bankType)
        {
            if (bankType == BankType.None) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.InMenus, () => EnterBank(bankType));
            shopper.SetBankType(bankType);
            tradeData = new TradeData(bankType);
            currentPlayerState.EnterTrade(this);
        }

        public void EnterWorldOptions()
        {
            optionStateType = OptionStateType.WorldOptions;
            currentPlayerState.EnterOptions(this);
        }

        public void EnterEscapeMenu()
        {
            optionStateType = OptionStateType.EscapeMenu;
            currentPlayerState.EnterOptions(this);
        }

        public void EnterCutscene(bool playerVisible = true, bool canMove = false)
        {
            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.InCutScene, () => EnterCutscene(playerVisible));
            visibleDuringCutscene = playerVisible;
            canMoveInCutscene = canMove && playerVisible;
            currentPlayerState.EnterCutScene(this);
        }
        #endregion

        #region UtilityTransition
        public bool InZoneTransition() => currentTransitionType == TransitionType.Zone;
        public bool IsZoneTransitionComplete() => zoneTransitionComplete;
        public void SetZoneTransitionStatus(bool complete)
        {
            zoneTransitionComplete = complete;
        }
        public void ConfirmTransitionType()
        {
            currentTransitionType = transitionTypeUnderConsideration;
        }

        private static bool IsBattleTransition(TransitionType transitionType) => transitionType is TransitionType.BattleNeutral or TransitionType.BattleGood or TransitionType.BattleBad;
        public bool InBattleEntryTransition() => IsBattleTransition(currentTransitionType);
        public bool InBattleExitTransition() => currentTransitionType == TransitionType.BattleComplete;
        #endregion

        #region UtilityCombat
        public bool IsAnyPartyMemberAlive() => partyCombatConduit.IsAnyMemberAlive();
        public bool IsPlayerFearsome(CombatParticipant combatParticipant) => partyCombatConduit.IsFearsome(combatParticipant);

        public bool AreCombatParticipantsValid(bool announceCannotFight = false)
        {
            if (!partyCombatConduit.IsAnyMemberAlive()) { if (announceCannotFight) { EnterDialogue(messageCannotFight); } return false; }
            return !enemiesUnderConsideration.All(x => x.IsDead());
        }

        public void AddEnemiesUnderConsideration()
        {
            foreach (CombatParticipant enemy in enemiesUnderConsideration)
            {
                if (enemiesInTransition.Count > maxEnemiesPerCombat) { return; }

                if (!enemiesInTransition.Contains(enemy))
                {
                    enemiesInTransition.Add(enemy);
                }
            }
        }

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

            combatFadeComplete = false;
            var faderEventTriggers = new FaderEventTriggers(null, () => OnBattleEntryPeak(currentTransitionType), null, () => OnBattleEntryComplete(currentTransitionType));
            bool faderInitiated = Fader.StartStandardFade(currentTransitionType, faderEventTriggers);

            if (!faderInitiated) { combatFadeComplete = true; }
            return faderInitiated;
        }

        public bool IsCombatFadeComplete() => combatFadeComplete;

        public bool EndBattleSequence()
        {
            // Edge case on improper game exit, starting Coroutine on object if it's undergoing destruction throws error
            if (this == null || gameObject == null) { return false; }

            currentTransitionType = TransitionType.BattleComplete;
            var faderEventTriggers = new FaderEventTriggers(null, OnBattleExitPeak, null, OnBattleExitComplete);
            
            return Fader.StartStandardFade(currentTransitionType, faderEventTriggers);
        }

        private void HandleCombatMessages(BattleStateChangedEvent battleStateChangedEvent)
        {
            BattleState battleState = battleStateChangedEvent.battleState;

            if (battleState != BattleState.Complete) { return; }
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleCombatMessages);

            currentTransitionType = TransitionType.BattleComplete;
            currentPlayerState.EnterTransition(this);
        }
        
        private void OnBattleEntryPeak(TransitionType transitionType)
        {
            if (battleUIPrefab != null) { Instantiate(battleUIPrefab); }
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.EntryPeak, enemiesInTransition, transitionType));
        }

        private void OnBattleEntryComplete(TransitionType transitionType)
        {
            combatFadeComplete = true;
            currentPlayerState.EnterCombat(this);
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.EntryComplete, enemiesInTransition, transitionType));
        }

        private void OnBattleExitPeak()
        {
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.ExitPeak));
            StartCoroutine(TimedCollisionDisable());
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

        public bool StartDialogueSequence()
        {
            if (dialogueData == null) { return false; }

            DialogueDataType dialogueDataType = dialogueData.dialogueDataType;
            switch (dialogueDataType)
            {
                case DialogueDataType.StandardDialogue:
                    dialogueController?.InitiateConversation(dialogueData.aiConversant, dialogueData.dialogue);
                    break;
                case DialogueDataType.SimpleText:
                    dialogueController?.InitiateSimpleMessage(dialogueData.message);
                    break;
                case DialogueDataType.SimpleChoice:
                    dialogueController?.InitiateSimpleOption(dialogueData.message, dialogueData.choiceActionPairs);
                    break;
                default:
                    return false;
            }
            return true;
        }
        #endregion

        #region UtilityTrade
        public bool StartTradeSequence()
        {
            if (tradeData == null) { return false; }
            switch (tradeData.tradeDataType)
            {
                case TradeDataType.Shop:
                    Instantiate(shopSelectPrefab, worldCanvas.gameObject.transform);
                    break;
                case TradeDataType.Bank:
                    Instantiate(cashTransferPrefab, worldCanvas.gameObject.transform);
                    break;
                case TradeDataType.None:
                    return false;
            }
            return true;
        }
        #endregion

        #region UtilityOption
        public bool StartOptionSequence()
        {
            switch (optionStateType)
            {
                case OptionStateType.WorldOptions:
                    Instantiate(worldOptionsPrefab, worldCanvas.gameObject.transform);
                    break;
                case OptionStateType.EscapeMenu:
                    Instantiate(escapeMenuPrefab, worldCanvas.gameObject.transform);
                    break;
                case OptionStateType.None:
                default:
                    return false;
            }
            return true;
        }

        #endregion

        #region UtilityGeneral
        public void TogglePlayerVisibility(bool? enable = null)
        {
            bool visible = enable ?? visibleDuringCutscene;

            if (party != null)
            {
                party.TogglePartyVisible(visible);
            }
            if (partyAssist != null)
            {
                partyAssist.TogglePartyVisible(visible);
            }
        }
        
        private IEnumerator TimedCollisionDisable()
        {
            SetPlayerImmunity(true);
            yield return new WaitForSeconds(immunityTimePostCombat);
            SetPlayerImmunity(false);
        }
        
        public void QueueActionUnderConsideration()
        {
            if (actionUnderConsideration?.action == null) { return; }
            queuedActions.Push(actionUnderConsideration);
        }

        private void PopQueuedAction()
        {
            if (queuedActions.Count == 0) { return; }
            
            PlayerStateTypeActionPair nextQueuedAction = queuedActions.Pop();
            nextQueuedAction.action?.Invoke();

            if (nextQueuedAction.playerStateType == PlayerStateType.InBattle)
            {
                // On combat allow chained queues (e.g. multiple combat instantiation while in dialogue)
                ChainQueuedCombatAction();
            }
        }

        private void ChainQueuedCombatAction()
        {
            if (queuedActions.Count > 0 && queuedActions.Peek().playerStateType == PlayerStateType.InBattle)
            {
                PopQueuedAction();
            }
        }

        public void ClearPlayerStateMemory()
        {
            // Kill controllers
            battleController = null;
            dialogueController = null;

            // Clear State
            actionUnderConsideration = null;

            transitionTypeUnderConsideration = TransitionType.None;
            currentTransitionType = TransitionType.None;
            zoneTransitionComplete = true;

            canMoveInCutscene = false;
            visibleDuringCutscene = true;

            combatFadeComplete = false;
            enemiesUnderConsideration.Clear();
            enemiesInTransition.Clear();

            dialogueData = null;

            shopper?.SetShop(null);
            shopper?.SetBankType(BankType.None);
            tradeData = null;

            optionStateType = OptionStateType.None;
        }
        #endregion
    }
}
