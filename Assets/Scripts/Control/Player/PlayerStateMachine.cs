using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using Frankie.Control.PlayerStates;
using Frankie.Combat;
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
        [SerializeField] BattleController battleControllerPrefab = null;
        [SerializeField] DialogueController dialogueControllerPrefab = null;
        [Header("Menu Game Objects")]
        [SerializeField] GameObject shopSelectPrefab = null;
        [SerializeField] GameObject cashTransferPrefab = null;
        [SerializeField] GameObject worldOptionsPrefab = null;
        [SerializeField] GameObject escapeMenuPrefab = null;
        [Header("Messages")]
        [SerializeField] string messageCannotFight = "You are wounded and cannot fight.";
        [Header("Parameters")]
        [SerializeField] int maxEnemiesPerCombat = 12;

        // State Information
        // Player
        IPlayerState currentPlayerState = new WorldState();
        // Queue
        PlayerStateTypeActionPair actionUnderConsideration = null;
        Stack<PlayerStateTypeActionPair> queuedActions = new Stack<PlayerStateTypeActionPair>();
        bool readyToPopQueue = false;
        // Transition
        TransitionType transitionTypeUnderConsideration = TransitionType.None;
        TransitionType currentTransitionType = TransitionType.None;
        bool zoneTransitionComplete = true;

        // CutScene
        bool visibleDuringCutscene = true;
        // Combat
        bool combatFadeComplete = false;
        List<CombatParticipant> enemiesUnderConsideration = new List<CombatParticipant>();
        List<CombatParticipant> enemiesInTransition = new List<CombatParticipant>();
        // Dialogue
        DialogueData dialogueData = null;
        // Trade
        TradeData tradeData = null;
        // Option
        OptionStateType optionStateType = default;

        // Cached References -- Persistent
        Party party = null;
        PartyAssist partyAssist = null;
        PartyCombatConduit partyCombatConduit = null;
        Shopper shopper = null;
        WorldCanvas worldCanvas = null;
        // Cached References -- State Dependent
        BattleController battleController = null;
        DialogueController dialogueController = null;

        // Events
        public event Action<PlayerStateType> playerStateChanged;

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
        static PlayerStateType TranslatePlayerState(IPlayerState playerState)
        {
            Type playerStateType = playerState.GetType();
            if (playerStateType == typeof(WorldState))
            {
                return PlayerStateType.inWorld;
            }
            else if (playerStateType == typeof(TransitionState))
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
            return PlayerStateType.inWorld;
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
        private void UpdateReferencesForNewScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas")?.GetComponent<WorldCanvas>();
        }

        void IPlayerStateContext.SetPlayerState(IPlayerState playerState)
        {
            PlayerStateType playerStateType = TranslatePlayerState(playerState);
            UnityEngine.Debug.Log($"Updating player state to: {Enum.GetName(typeof(PlayerStateType), playerStateType)}");

            currentPlayerState = playerState;
            playerStateChanged?.Invoke(playerStateType);

            readyToPopQueue = playerStateType == PlayerStateType.inWorld; 
                // Pop on update to prevent same-frame multi-state change
                // Otherwise can experience bugs with controller spawning while deconstructing conflicting w/ singleton logic

            if (playerStateType == PlayerStateType.inTransition && InBattleEntryTransition()) { ChainQueuedCombatAction(); }
                // Required to allow swarm / multi-battle entry on same-frame
        }

        public void SetPostDialogueCallbackActions(InteractionEvent interactionEvent)
        {
            DialogueController dialogueController = GetCurrentDialogueController();
            if (dialogueController != null && interactionEvent != null)
            {
                dialogueController.SetDestroyCallbackActions(interactionEvent);
            }
        }

        public Party GetParty() // TODO:  Refactor, Demeter
        {
            return party;
        }

        public DialogueController GetCurrentDialogueController()
        {
            return dialogueController;
        }
        #endregion

        #region StateTransitions
        public void EnterWorld()
        {
            currentPlayerState.EnterWorld(this);
        }

        public void EnterZoneTransition()
        {
            actionUnderConsideration = new PlayerStateTypeActionPair(PlayerStateType.inTransition, () => EnterZoneTransition());
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
                BattleController existingBattleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
                if (existingBattleController == null)
                {
                    battleController = Instantiate(battleControllerPrefab);
                }
                else
                {
                    battleController = existingBattleController;
                }
            }
            battleController.battleStateChanged += HandleCombatMessages;
        }

        public bool StartBattleSequence()
        {
            Fader fader = FindAnyObjectByType<Fader>();
            if (fader == null || fader.IsFading() == true) { return false; } // Safety against missing fader
            if (fader.IsFading() == true) { return true; } // Safety against multiple fading routines

            StartCoroutine(QueueBattleTransition(fader, currentTransitionType));
            return true;
        }

        public bool IsCombatFadeComplete()
        {
            return combatFadeComplete;
        }

        public bool EndBattleSequence()
        {
            Fader fader = FindAnyObjectByType<Fader>();
            if (fader == null || fader.IsFading() == true) { return false; } // Safety against missing fader
            if (fader.IsFading() == true) { return true; } // Safety against multiple fading routines

            StartCoroutine(QueueExitCombat(fader));
            return true;
        }

        private void HandleCombatMessages(BattleState battleState, BattleOutcome battleOutcome)
        {
            if (battleState != BattleState.Complete) { return; }
            battleController.battleStateChanged -= HandleCombatMessages;

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
            yield return fader.QueueFadeExit(currentTransitionType);

            currentPlayerState.EnterWorld(this);
        }
        #endregion

        #region UtilityDialogue
        public void SetupDialogueController()
        {
            if (dialogueController == null)
            {
                DialogueController existingDialogueController = GameObject.FindGameObjectWithTag("DialogueController")?.GetComponent<DialogueController>();
                if (existingDialogueController == null)
                {
                    dialogueController = Instantiate(dialogueControllerPrefab);
                }
                else
                {
                    dialogueController = existingDialogueController;
                }
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
            bool visible = enable == null ? visibleDuringCutscene : (bool)enable;

            if (party != null)
            {
                party.TogglePartyVisible(visible);
            }
            if (partyAssist != null)
            {
                partyAssist.TogglePartyVisible(visible);
            }
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