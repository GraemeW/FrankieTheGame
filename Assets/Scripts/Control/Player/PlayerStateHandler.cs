using Frankie.Combat;
using Frankie.ZoneManagement;
using Frankie.Speech;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace Frankie.Control
{
    [RequireComponent(typeof(Party))]
    [RequireComponent(typeof(Shopper))]
    public class PlayerStateHandler : MonoBehaviour
    {
        // Tunables
        [Header("Other Controller Prefabs")]
        [SerializeField] BattleController battleControllerPrefab = null;
        [SerializeField] DialogueController dialogueControllerPrefab = null;
        [Header("Menu Game Objects")]
        [SerializeField] GameObject shopSelectPrefab = null;
        [SerializeField] GameObject worldOptionsPrefab = null;
        [SerializeField] GameObject escapeMenuPrefab = null;
        [Header("Messages")]
        [SerializeField] string messageCannotFight = "You are wounded and cannot fight.";

        // State
        PlayerState playerState = PlayerState.inWorld;
        bool stateChangedThisFrame = false;
        Stack<Action> queuedActions = new Stack<Action>();
        TransitionType transitionType = TransitionType.None;
        BattleController battleController = null;
        DialogueController dialogueController = null;
        List<CombatParticipant> enemiesInTransition = new List<CombatParticipant>();

        // Cached References
        Party party = null;
        Shopper shopper = null;
        WorldCanvas worldCanvas = null;

        // Events
        public event Action<PlayerState> playerStateChanged;

        #region UnityStandardMethods
        private void Awake()
        {
            party = GetComponent<Party>();
            shopper = GetComponent<Shopper>();
        }

        private void Update()
        {
            KillRogueControllers(playerState);
            PullFromActionQueue();
        }

        private void LateUpdate()
        {
            stateChangedThisFrame = false;
        }
        #endregion

        #region SettersGetters
        public void SetWorldCanvas()
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas")?.GetComponent<WorldCanvas>();
        }

        public void SetPlayerState(PlayerState playerState)
        {
            this.playerState = playerState;
            stateChangedThisFrame = true;

            playerStateChanged?.Invoke(playerState);
        }

        private bool PullFromActionQueue()
        {
            if (queuedActions.Count == 0) { return false; }

            if (playerState == PlayerState.inWorld)
            {
                queuedActions.Pop().Invoke();
                return true;
            }

            return false;
        }

        public PlayerState GetPlayerState()
        {
            return playerState;
        }

        public TransitionType GetTransitionType()
        {
            return transitionType;
        }

        public Party GetParty()
        {
            return party;
        }

        public DialogueController GetCurrentDialogueController()
        {
            return dialogueController;
        }

        public BattleController GetCurrentBattleController()
        {
            return battleController;
        }

        private DialogueController GetUniqueDialogueController()
        {
            if (dialogueController != null) { return dialogueController; }

            DialogueController existingDialogueController = GameObject.FindGameObjectWithTag("DialogueController")?.GetComponent<DialogueController>();
            if (existingDialogueController == null)
            {
                dialogueController = Instantiate(dialogueControllerPrefab);
            }
            else
            {
                dialogueController = existingDialogueController;
            }

            return dialogueController;
        }

        private BattleController GetUniqueBattleController()
        {
            if (battleController != null) { return battleController; }

            BattleController existingBattleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
            if (existingBattleController == null)
            {
                battleController = Instantiate(battleControllerPrefab);
            }
            else
            {
                battleController = existingBattleController;
            }

            return battleController;
        }
        #endregion

        #region StateTransitions
        public bool EnterCombat(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            if (ShouldQueueAction(transitionType)) { queuedActions.Push(() => EnterCombat(enemies, transitionType)); return false; }

            if (!party.IsAnyMemberAlive()) { EnterDialogue(messageCannotFight); return false; }
            if (enemies.All(x => x.IsDead())) { return false; }

            if (GetPlayerState() == PlayerState.inTransition)
            {
                AddToEnemiesInTransition(enemies);
                return true;
            }
            else if (GetPlayerState() == PlayerState.inWorld)
            {
                battleController = GetUniqueBattleController();
                battleController.battleStateChanged += ExitCombat;

                enemiesInTransition.Clear();
                AddToEnemiesInTransition(enemies);

                StartCoroutine(QueueBattleTransition(transitionType));
                SetPlayerState(PlayerState.inTransition);
                return true;
            }

            return false;
        }

        public void ExitCombat(BattleState battleState)
        {
            if (battleState != BattleState.Complete) { return; }
            battleController.battleStateChanged -= ExitCombat;

            StartCoroutine(QueueExitCombat());
        }

        public void EnterDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            if (ShouldQueueAction(TransitionType.None)) { queuedActions.Push(() => EnterDialogue(newConversant, newDialogue)); return; }
            if (!IsDialoguePossible()) { return; }

            dialogueController = Instantiate(dialogueControllerPrefab);
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateConversation(newConversant, newDialogue);

            SetPlayerState(PlayerState.inDialogue);
        }

        public void EnterDialogue(string message)
        {
            if (ShouldQueueAction(TransitionType.None)) { queuedActions.Push(() => EnterDialogue(message));  return; }
            if (!IsDialoguePossible()) { return; }

            dialogueController = GetUniqueDialogueController();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateSimpleMessage(message);

            SetPlayerState(PlayerState.inDialogue);
        }

        public void EnterDialogue(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            if (ShouldQueueAction(TransitionType.None)) { queuedActions.Push(() => EnterDialogue(message, choiceActionPairs));  return; }
            if (!IsDialoguePossible()) { return; }

            dialogueController = GetUniqueDialogueController();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateSimpleOption(message, choiceActionPairs);

            SetPlayerState(PlayerState.inDialogue);
        }

        public void EnterShop(Shop shop)
        {
            if (shopper == null || shop == null) { return; }

            shopper.SetShop(shop);
            Instantiate(shopSelectPrefab, worldCanvas.gameObject.transform);
            SetPlayerState(PlayerState.inMenus);
        }

        public void ExitShop()
        {
            shopper.SetShop(null);
            SetPlayerState(PlayerState.inWorld);
        }

        public void ExitDialogue()
        {
            dialogueController = null;
            if (playerState == PlayerState.inDialogue)
            {
                SetPlayerState(PlayerState.inWorld);
            }
        }

        public void EnterWorldOptions()
        {
            Instantiate(worldOptionsPrefab, worldCanvas.gameObject.transform);
            SetPlayerState(PlayerState.inMenus);
        }

        public void ExitWorldOptions()
        {
            SetPlayerState(PlayerState.inWorld);
        }

        public void EnterEscapeMenu()
        {
            Instantiate(escapeMenuPrefab, worldCanvas.gameObject.transform);
            SetPlayerState(PlayerState.inMenus);
        }

        public void ExitEscapeMenu()
        {
            SetPlayerState(PlayerState.inWorld);
        }
        #endregion

        #region PrivateMethods
        // Combat
        private IEnumerator QueueBattleTransition(TransitionType transitionType)
        {
            Fader fader = FindObjectOfType<Fader>();
            if (fader.IsFading() == true) { yield break; }

            this.transitionType = transitionType;
            SetPlayerState(PlayerState.inTransition);

            yield return fader.QueueFadeEntry(transitionType);
            battleController.Setup(enemiesInTransition, transitionType);
            yield return fader.QueueFadeExit(transitionType);

            SetPlayerState(PlayerState.inBattle);
        }

        private void AddToEnemiesInTransition(List<CombatParticipant> enemies)
        {
            foreach (CombatParticipant enemy in enemies)
            {
                if (!enemiesInTransition.Contains(enemy))
                {
                    enemiesInTransition.Add(enemy);
                }
            }
        }

        private IEnumerator QueueExitCombat()
        {
            Fader fader = FindObjectOfType<Fader>();
            if (fader.IsFading() == true) { yield break; }

            transitionType = TransitionType.BattleComplete;
            SetPlayerState(PlayerState.inTransition);

            yield return fader.QueueFadeEntry(transitionType);
            // TODO:  Handling for party death
            Destroy(battleController.gameObject);
            battleController = null;
            yield return fader.QueueFadeExit(transitionType);

            SetPlayerState(PlayerState.inWorld);
        }

        // Dialogue
        private bool IsDialoguePossible()
        {
            if (GetPlayerState() != PlayerState.inTransition)
            {
                return true;
            }
            return false;
        }

        // Utility
        private bool ShouldQueueAction(TransitionType transitionType)
        {
            // Handling for same-frame contact swarm mechanic
            if (HadMultipleCombatEntriesOnSameFrame(transitionType)) { return false; }

            // Queue otherwise
            if (stateChangedThisFrame) { return true; }
            if (playerState == PlayerState.inBattle || playerState == PlayerState.inDialogue)
            {
                return true;
            }

            return false;
        }

        private bool HadMultipleCombatEntriesOnSameFrame(TransitionType transitionType)
        {
            return stateChangedThisFrame
                && (transitionType == TransitionType.BattleNeutral || transitionType == TransitionType.BattleGood || transitionType == TransitionType.BattleBad)
                && (GetPlayerState() == PlayerState.inTransition);
        }

        private void KillRogueControllers(PlayerState playerState)
        {
            if (playerState == PlayerState.inWorld)
            {
                if (battleController != null)
                {
                    ExitCombat(BattleState.Complete);
                }
                if (dialogueController != null)
                {
                    ExitDialogue();
                }
            }
            else if (playerState == PlayerState.inBattle)
            {
                if (dialogueController != null) // Dialogue controller active during battle
                {
                    ExitDialogue();
                }

                if (battleController == null) // Stuck in battle with no battle controller present
                {
                    SetPlayerState(PlayerState.inWorld);
                }
            }
            else if (playerState == PlayerState.inDialogue)
            {
                if (battleController != null) // Battle controller active during dialogue
                {
                    ExitCombat(BattleState.Complete);
                }

                if (dialogueController == null)
                {
                    SetPlayerState(PlayerState.inWorld);
                }
            }
        }
        #endregion
    }
}