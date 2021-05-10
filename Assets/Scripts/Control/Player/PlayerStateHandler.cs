using Frankie.Combat;
using Frankie.ZoneManagement;
using Frankie.Speech;
using Frankie.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        public void EnterCombat(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            if (!party.IsAnyMemberAlive()) { OpenSimpleDialogue(messageCannotFight); return; }

            // TODO:  Concept of 'pre-battle' where enemies can pile on ++ count up list of enemies -> transfer to battle
            this.transitionType = transitionType;

            battleController = GetUniqueBattleController();
            battleController.Setup(enemies, transitionType);

            Fader fader = FindObjectOfType<Fader>();
            fader.UpdateFadeState(transitionType);

            battleController.battleStateChanged += HandleCombatComplete;

            SetPlayerState(PlayerState.inBattle);
        }

        public void HandleCombatComplete(BattleState battleState)
        {
            if (battleState != BattleState.Complete) { return; }
            battleController.battleStateChanged -= HandleCombatComplete;

            Fader fader = FindObjectOfType<Fader>();
            fader.battleUIStateChanged += ExitCombat;
            transitionType = TransitionType.BattleComplete;
            fader.UpdateFadeState(transitionType);
        }

        public void ExitCombat(bool isBattleCanvasEnabled)
        {
            if (!isBattleCanvasEnabled)
            {
                // TODO:  Handling for party death
                FindObjectOfType<Fader>().battleUIStateChanged -= ExitCombat;
                Destroy(battleController.gameObject);
                battleController = null;

                if (playerState == PlayerState.inBattle)
                {
                    SetPlayerState(PlayerState.inWorld);
                }
            }
        }

        public void EnterDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            GameObject dialogueControllerObject = Instantiate(dialogueControllerPrefab);
            dialogueController = dialogueControllerObject.GetComponent<DialogueController>();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateConversation(newConversant, newDialogue);

            SetPlayerState(PlayerState.inDialogue);
        }

        public void OpenSimpleDialogue(string message)
        {
            dialogueController = GetUniqueDialogueController();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateSimpleMessage(message);

            if (playerState != PlayerState.inTransition) // do not override state if in transition
            {
                SetPlayerState(PlayerState.inDialogue);
            }
        }

        public void OpenSimpleChoiceDialogue(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            dialogueController = GetUniqueDialogueController();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateSimpleOption(message, choiceActionPairs);

            if (playerState != PlayerState.inTransition) // do not override state if in transition
            {
                SetPlayerState(PlayerState.inDialogue);
            }
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
                if (dialogueController != null)
                {
                    ExitDialogue();
                }
            }
            else if (playerState == PlayerState.inDialogue)
            {
                if (battleController != null)
                {
                    HandleCombatComplete(BattleState.Complete);
                }
            }
        }
    }

}