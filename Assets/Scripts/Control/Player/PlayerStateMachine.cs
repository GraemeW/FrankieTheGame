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
using Frankie.Utils;

namespace Frankie.Control
{
    [RequireComponent(typeof(Party))]
    [RequireComponent(typeof(Shopper))]
    public class PlayerStateMachine : MonoBehaviour, IPlayerStateContext
    {
        // Tunables
        [Header("Other Controller Prefabs")]
        [SerializeField] private BattleController battleControllerPrefab;
        [SerializeField] private DialogueController dialogueControllerPrefab;
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
        private readonly Stack<PlayerStateTypeActionPair> queuedActions = new Stack<PlayerStateTypeActionPair>();
        private bool readyToPopQueue = false;
        // Transition
        private TransitionType transitionTypeUnderConsideration = TransitionType.None;
        private TransitionType currentTransitionType = TransitionType.None;
        private bool zoneTransitionComplete = true;

        // CutScene
        private bool visibleDuringCutscene = true;
        // Combat
        private bool combatFadeComplete = false;
        private readonly List<CombatParticipant> enemiesUnderConsideration = new List<CombatParticipant>();
        private readonly List<CombatParticipant> enemiesInTransition = new List<CombatParticipant>();
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
        private WorldCanvas worldCanvas;
        // Cached References -- State Dependent
        private BattleController battleController;
        private DialogueController dialogueController;

        // Events
        public event Action<PlayerStateType> playerStateChanged;
        public event Action<int> playerLayerChanged;

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
            if (playerStateType == typeof(TransitionState))
            {
                return PlayerStateType.inTransition;
            }
            else if (playerStateType == typeof(CombatState))
            {
                return PlayerStateType.inBattle;
            }
            else if (playerStateType == typeof(DialogueState))
            {
                return PlayerStateType.inDialogue;
            }
            else if (playerStateType == typeof(TradeState) || playerStateType == typeof(OptionState))
            {
                return PlayerStateType.inMenus;
            }
            else if (playerStateType == typeof(CutSceneState))
            {
                return PlayerStateType.inCutScene;
            }
            return PlayerStateType.inWorld; // Default:  typeof(WorldState)
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
            playerStateChanged?.Invoke(playerStateType);

            readyToPopQueue = playerStateType == PlayerStateType.inWorld;
            // Pop on update to prevent same-frame multi-state change
            // Otherwise can experience bugs with controller spawning while deconstructing conflicting w/ singleton logic

            if (playerStateType == PlayerStateType.inTransition && InBattleEntryTransition()) { ChainQueuedCombatAction(); }
            // Required to allow swarm / multi-battle entry on same-frame
        }
        
        public Party GetParty() => party;
        
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
            int layer = enablePlayerImmunity ? Player.GetImmunePlayerLayer() :  Player.GetPlayerLayer();
            gameObject.layer = layer;
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = layer;
            }
            playerLayerChanged?.Invoke(layer);
        }
        #endregion

        #region StateTransitions
        public void EnterWorld()
        {
            currentPlayerState.EnterWorld(this);
        }

        public void EnterZoneTransition()
        {
            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inTransition, EnterZoneTransition);
            currentTransitionType = TransitionType.Zone;
            zoneTransitionComplete = false;
            currentPlayerState.EnterTransition(this);
        }

        public void EnterCombat(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            if (enemies == null || enemies.Count == 0 || !IsBattleTransition(transitionType)) { return; }
            //Useful Debug:
            //UnityEngine.Debug.Log($"Request to enter combat by {enemies.FirstOrDefault().GetCombatName()}");

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inBattle, () => EnterCombat(enemies, transitionType));
            enemiesUnderConsideration.Clear();
            enemiesUnderConsideration.AddRange(enemies);
            transitionTypeUnderConsideration = transitionType;

            currentPlayerState.EnterCombat(this);
        }

        public void EnterDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            if (newConversant == null || newDialogue == null) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inDialogue, () => EnterDialogue(newConversant, newDialogue));
            dialogueData = new DialogueData(newConversant, newDialogue);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterDialogue(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inDialogue, () => EnterDialogue(message));
            dialogueData = new DialogueData(message);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterDialogue(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            if (choiceActionPairs == null || choiceActionPairs.Count == 0) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inDialogue, () => EnterDialogue(message, choiceActionPairs));
            dialogueData = new DialogueData(message, choiceActionPairs);
            currentPlayerState.EnterDialogue(this);
        }

        public void EnterShop(Shop shop)
        {
            if (shopper == null || shop == null) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inMenus, () => EnterShop(shop));
            shopper.SetShop(shop);
            tradeData = new TradeData(shop.GetShopType());
            currentPlayerState.EnterTrade(this);
        }

        public void EnterBank(BankType bankType)
        {
            if (bankType == BankType.None) { return; }

            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inMenus, () => EnterBank(bankType));
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

        public void EnterCutscene(bool playerVisible = true)
        {
            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inCutScene, () => EnterCutscene(playerVisible));
            visibleDuringCutscene = playerVisible;
            currentPlayerState.EnterCutScene(this);
        }
        #endregion

        #region UtilityTransition
        public void ConfirmTransitionType()
        {
            currentTransitionType = transitionTypeUnderConsideration;
        }

        public bool InZoneTransition()
        {
            return currentTransitionType == TransitionType.Zone;
        }

        public bool IsZoneTransitionComplete()
        {
            return zoneTransitionComplete;
        }

        public void SetZoneTransitionStatus(bool complete)
        {
            zoneTransitionComplete = complete;
        }

        public bool InBattleEntryTransition()
        {
            return IsBattleTransition(currentTransitionType);
        }

        public bool InBattleExitTransition()
        {
            return currentTransitionType == TransitionType.BattleComplete;
        }

        private bool IsBattleTransition(TransitionType transitionType)
        {
            return (transitionType == TransitionType.BattleNeutral || transitionType == TransitionType.BattleGood || transitionType == TransitionType.BattleBad);
        }
        #endregion

        #region UtilityCombat
        public bool AreCombatParticipantsValid(bool announceCannotFight = false)
        {
            if (!partyCombatConduit.IsAnyMemberAlive()) { if (announceCannotFight) { EnterDialogue(messageCannotFight); } return false; }
            if (enemiesUnderConsideration.All(x => x.IsDead())) { return false; }
            return true;
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
            Fader fader = Fader.FindFader();
            if (fader == null || fader.IsFading()) { return false; }

            StartCoroutine(QueueBattleTransition(fader, currentTransitionType));
            return true;
        }

        public bool IsCombatFadeComplete()
        {
            return combatFadeComplete;
        }

        public bool EndBattleSequence()
        {
            Fader fader = Fader.FindFader();
            if (fader == null || fader.IsFading()) { return false; }

            StartCoroutine(QueueExitCombat(fader));
            return true;
        }

        private void HandleCombatMessages(BattleStateChangedEvent battleStateChangedEvent)
        {
            BattleState battleState = battleStateChangedEvent.battleState;

            if (battleState != BattleState.Complete) { return; }
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleCombatMessages);

            currentTransitionType = TransitionType.BattleComplete;
            currentPlayerState.EnterTransition(this);
        }

        private IEnumerator QueueBattleTransition(Fader fader, TransitionType transitionType)
        {
            combatFadeComplete = false;

            yield return fader.QueueFadeEntry(transitionType);
            battleController.QueueCombatInitiation(enemiesInTransition, transitionType);
            yield return fader.QueueFadeExit(transitionType);

            combatFadeComplete = true;
            currentPlayerState.EnterCombat(this);
        }

        private IEnumerator QueueExitCombat(Fader fader)
        {
            currentTransitionType = TransitionType.BattleComplete;

            yield return fader.QueueFadeEntry(currentTransitionType);
            Destroy(battleController.gameObject);
            StartCoroutine(TimedCollisionDisable());
            yield return fader.QueueFadeExit(currentTransitionType);
            
            BattleEventBus<BattleExitEvent>.Raise(new BattleExitEvent());
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
            if (actionUnderConsideration == null || actionUnderConsideration.action == null) { return; }
            queuedActions.Push(actionUnderConsideration);
        }

        private void PopQueuedAction()
        {
            if (queuedActions.Count > 0)
            {
                PlayerStateTypeActionPair nextQueuedAction = queuedActions.Pop();
                nextQueuedAction.action?.Invoke();

                if (nextQueuedAction.playerStateType == PlayerStateType.inBattle)
                {
                    // On combat allow chained queues (e.g. multiple combat instantiation while in dialogue)
                    ChainQueuedCombatAction();
                }

            }
        }

        private void ChainQueuedCombatAction()
        {
            if (queuedActions.Count > 0 && queuedActions.Peek().playerStateType == PlayerStateType.inBattle)
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
