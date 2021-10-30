using Frankie.Combat;
using Frankie.ZoneManagement;
using Frankie.Speech;
using Frankie.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Frankie.Control
{
    public class PlayerStateHandler : MonoBehaviour
    {
        // Tunables
        [Header("Other Controller Prefabs")]
        [SerializeField] GameObject battleControllerPrefab = null;
        [SerializeField] GameObject dialogueControllerPrefab = null;
        [Header("World UI Game Objects")]
        [SerializeField] GameObject worldOptionsPrefab = null;
        [SerializeField] GameObject escapeMenuPrefab = null;
        [Header("Messages")]
        [SerializeField] string messageCannotFight = "You are wounded and cannot fight.";

        // State
        PlayerState playerState = PlayerState.inWorld;
        TransitionType transitionType = TransitionType.None;
        BattleController battleController = null;
        DialogueController dialogueController = null;
        List<CombatParticipant> enemiesInTransition = new List<CombatParticipant>();

        // Cached References
        Party party = null;
        WorldCanvas worldCanvas = null;

        // Events
        public event Action<PlayerState> playerStateChanged;

        // Public Functions
        public void SetWorldCanvas()
        {
            if (worldCanvas == null)
            {
                worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas").GetComponent<WorldCanvas>();
            }
        }

        public void SetPlayerState(PlayerState playerState)
        {
            this.playerState = playerState;
            if (playerStateChanged != null)
            {
                playerStateChanged.Invoke(playerState);
            }
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

        public bool EnterCombat(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            if (!party.IsAnyMemberAlive()) { EnterDialogue(messageCannotFight); return false; }
            if (GetPlayerState() == PlayerState.inDialogue) { ExitDialogue(); }

            if (GetPlayerState() == PlayerState.inWorld)
            {
                battleController = GetUniqueBattleController();
                battleController.battleStateChanged += HandleCombatComplete;

                enemiesInTransition.Clear();
                AddToEnemiesInTransition(enemies);

                StartCoroutine(QueueBattleTransition(transitionType));
                return true;
            }
            else if (GetPlayerState() == PlayerState.inTransition)
            {
                AddToEnemiesInTransition(enemies);
                return true;
            }

            return false;
        }

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

        public void HandleCombatComplete(BattleState battleState)
        {
            if (battleState != BattleState.Complete) { return; }
            battleController.battleStateChanged -= HandleCombatComplete;

            StartCoroutine(QueueExitCombat());
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

        public void EnterDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            if (!IsDialoguePossible()) { return; }

            GameObject dialogueControllerObject = Instantiate(dialogueControllerPrefab);
            dialogueController = dialogueControllerObject.GetComponent<DialogueController>();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateConversation(newConversant, newDialogue);

            SetPlayerState(PlayerState.inDialogue);
        }

        public void EnterDialogue(string message)
        {
            if (!IsDialoguePossible()) { return; }

            dialogueController = GetUniqueDialogueController();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateSimpleMessage(message);

            SetPlayerState(PlayerState.inDialogue);
        }

        public void EnterDialogue(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            if (!IsDialoguePossible()) { return; }

            dialogueController = GetUniqueDialogueController();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateSimpleOption(message, choiceActionPairs);

            SetPlayerState(PlayerState.inDialogue);
        }

        private bool IsDialoguePossible()
        {
            if (GetPlayerState() != PlayerState.inTransition)
            {
                return true;
            }
            return false;
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
            SetPlayerState(PlayerState.inOptions);
        }

        public void ExitWorldOptions()
        {
            SetPlayerState(PlayerState.inWorld);
        }

        public void EnterEscapeMenu()
        {
            Instantiate(escapeMenuPrefab, worldCanvas.gameObject.transform);
            SetPlayerState(PlayerState.inOptions);
        }

        public void ExitEscapeMenu()
        {
            SetPlayerState(PlayerState.inWorld);
        }

        // Internal Functions
        private void Awake()
        {
            party = GetComponent<Party>();
        }

        private void Update()
        {
            KillRogueControllers(playerState);
        }

        private DialogueController GetUniqueDialogueController()
        {
            if (dialogueController != null) { return dialogueController; }

            GameObject dialogueControllerObject = GameObject.FindGameObjectWithTag("DialogueController");
            DialogueController existingDialogueController = null;
            if (dialogueControllerObject != null)
            {
                existingDialogueController = dialogueControllerObject.GetComponent<DialogueController>();
            }

            if (existingDialogueController == null)
            {
                GameObject newDialogueControllerObject = Instantiate(dialogueControllerPrefab);
                dialogueController = newDialogueControllerObject.GetComponent<DialogueController>();
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

            GameObject battleControllerObject = GameObject.FindGameObjectWithTag("BattleController");
            BattleController existingBattleControllerController = null;
            if (battleControllerObject != null)
            {
                existingBattleControllerController = battleControllerObject.GetComponent<BattleController>();
            }

            if (existingBattleControllerController == null)
            {
                GameObject newBattleControllerObject = Instantiate(battleControllerPrefab);
                battleController = newBattleControllerObject.GetComponent<BattleController>();
            }
            else
            {
                battleController = existingBattleControllerController;
            }

            return battleController;
        }

        private void KillRogueControllers(PlayerState playerState)
        {
            if (playerState == PlayerState.inWorld)
            {
                if (battleController != null)
                {
                    HandleCombatComplete(BattleState.Complete);
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
                    HandleCombatComplete(BattleState.Complete);
                }

                if (dialogueController == null)
                {
                    SetPlayerState(PlayerState.inWorld);
                }
            }
        }
    }

}