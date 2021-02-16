using Frankie.Core;
using Frankie.Stats;
using Frankie.Control;
using Frankie.Speech.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Speech
{
    public class DialogueController : MonoBehaviour, IPlayerInput
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] GameObject dialogueBoxPrefab = null;

        [Header("Interaction")]
        [SerializeField] string interactExecuteButton = "Fire1";
        [SerializeField] KeyCode interactExecuteKey = KeyCode.E;
        [SerializeField] KeyCode interactUp = KeyCode.W;
        [SerializeField] KeyCode interactLeft = KeyCode.A;
        [SerializeField] KeyCode interactRight = KeyCode.D;
        [SerializeField] KeyCode interactDown = KeyCode.S;

        // State
        Dialogue currentDialogue = null;
        DialogueNode currentNode = null;
        AIConversant currentConversant = null;
        DialogueNode highlightedNode = null;

        // Cached References
        WorldCanvas worldCanvas = null;
        PlayerController playerController = null;
        Party party = null;

        // Events
        public event Action<PlayerInputType> globalInput;
        public event Action<PlayerInputType> dialogueInput;
        public event Action<DialogueNode> highlightedNodeChanged;
        public event Action dialogueUpdated;

        // Methods
        public void Setup(WorldCanvas worldCanvas, PlayerController playerController, Party party)
        {
            this.worldCanvas = worldCanvas;
            this.playerController = playerController;
            this.party = party;
        }

        public void InitiateConversation(AIConversant newConversant, Dialogue newDialogue)
        {
            currentConversant = newConversant;
            currentDialogue = newDialogue;
            currentDialogue.OverrideSpeakerNames(GetPlayerName());

            currentNode = currentDialogue.GetRootNode();
            if (currentDialogue.skipRootNode) { Next(false); }

            Instantiate(dialogueBoxPrefab, worldCanvas.transform);
            TriggerEnterAction();
            if (dialogueUpdated != null)
            {
                dialogueUpdated.Invoke();
            }
        }

        public void EndConversation()
        {
            TriggerExitAction();
            currentConversant = null;
            currentDialogue = null;
            currentNode = null;
            if (dialogueUpdated != null)
            {
                dialogueUpdated.Invoke();
            }
            playerController.ExitDialogue();
            Destroy(gameObject);
        }

        private void Update()
        {
            // TODO:  Implement new unity input system
            PlayerInputType playerInputType = GetPlayerInput();
            if (InteractWithChoices(playerInputType)) { return; }
            if (InteractWithNext(playerInputType)) { return; }
            if (InteractWithGlobals()) { return; }
        }

        private bool InteractWithGlobals()
        {
            if (Input.GetButtonDown(interactExecuteButton) || Input.GetKeyDown(interactExecuteKey))
            {
                if (globalInput != null)
                {
                    globalInput.Invoke(PlayerInputType.Execute); // handle text skip on dialogue box
                    return true;
                }
            }
            return false;
        }

        private bool InteractWithNext(PlayerInputType playerInputType)
        {
            if (IsChoosing() || playerInputType == PlayerInputType.DefaultNone) { return false; }
            if (dialogueUpdated == null) { return false; }  // check if dialogue box can receive messages (toggled off during text-scans)

            if (playerInputType == PlayerInputType.Execute)
            {
                if (HasNext())
                {
                    Next();
                    return true;
                }
                else
                {
                    EndConversation();
                    return true;
                }
            }
            return false;
        }

        private bool InteractWithChoices(PlayerInputType playerInputType)
        {
            if (!IsChoosing() || playerInputType == PlayerInputType.DefaultNone) { return false; }
            if (dialogueUpdated == null) { return false; }  // check if dialogue box can receive messages (toggled off during text-scans)

            if (highlightedNode == null)
            {
                SetHighlightedNodeToDefault();
                return true;
            }
            
            if (playerInputType == PlayerInputType.Execute)
            {
                if (dialogueInput != null)
                {
                    dialogueInput.Invoke(playerInputType);
                }
                NextWithID(highlightedNode.name);
                highlightedNode = null;
                return true;
            }
            else if (playerInputType == PlayerInputType.NavigateUp || playerInputType == PlayerInputType.NavigateLeft
                || playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
            {
                List<DialogueNode> currentOptions = GetChoices().ToList();

                if (!currentOptions.Contains(highlightedNode))
                {
                    SetHighlightedNodeToDefault();
                    return true;
                }
                else
                {
                    HighlightNextNode(currentOptions, playerInputType);
                    return true;
                }
            }
            return false;
        }

        private void SetHighlightedNodeToDefault()
        {
            highlightedNode = GetChoices().FirstOrDefault();
            if (highlightedNodeChanged != null)
            {
                highlightedNodeChanged.Invoke(highlightedNode);
            }
        }

        private void HighlightNextNode(List<DialogueNode> currentOptions, PlayerInputType playerInputType)
        {
            int choiceIndex = currentOptions.IndexOf(highlightedNode);
            if (playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
            {
                if (choiceIndex + 1 >= currentOptions.Count) { choiceIndex = 0; }
                else { choiceIndex++; }
            }
            else if (playerInputType == PlayerInputType.NavigateUp || playerInputType == PlayerInputType.NavigateLeft)
            {
                if (choiceIndex <= 0) { choiceIndex = currentOptions.Count - 1; }
                else { choiceIndex--; }
            }

            highlightedNode = currentOptions[choiceIndex];
            if (highlightedNodeChanged != null)
            {
                highlightedNodeChanged.Invoke(highlightedNode);
            }
        }

        public PlayerInputType GetPlayerInput()
        {
            PlayerInputType input = PlayerInputType.DefaultNone;
            if (Input.GetKeyDown(interactUp))
            {
                input = PlayerInputType.NavigateUp;
            }
            else if (Input.GetKeyDown(interactLeft))
            {
                input = PlayerInputType.NavigateLeft;
            }
            else if (Input.GetKeyDown(interactRight))
            {
                input = PlayerInputType.NavigateRight;
            }
            else if (Input.GetKeyDown(interactDown))
            {
                input = PlayerInputType.NavigateDown;
            }
            else if (Input.GetButtonDown(interactExecuteButton) || Input.GetKeyDown(interactExecuteKey))
            {
                input = PlayerInputType.Execute;
            }
            return input;
        }

        public string GetPlayerName()
        {
            return party.GetParty()[0].GetCombatName();
        }

        public bool IsActive()
        {
            return (currentDialogue != null && currentNode != null);
        }

        public SpeakerType GetCurrentSpeakerType()
        {
            return currentNode.GetSpeakerType();
        }

        public SpeakerType GetNextSpeakerType()
        {
            return currentDialogue.GetNodeFromID(currentNode.GetChildren()[0]).GetSpeakerType();
        }

        public int GetChoiceCount()
        {
            return FilterOnCondition(currentNode.GetChildren()).Count();
        }

        public string GetCurrentSpeakerName()
        {
            return currentNode.GetSpeakerName();
        }

        public string GetText()
        {
            return currentNode.GetText();
        }

        public IEnumerable<DialogueNode> GetChoices()
        {
            foreach (string childID in FilterOnCondition(currentNode.GetChildren()))
            {
                yield return currentDialogue.GetNodeFromID(childID);
            }
        }

        public bool HasNext()
        {
            if (currentDialogue == null) { return false; }
            return FilterOnCondition(currentNode.GetChildren()).Count() > 0;
        }

        public bool IsChoosing()
        {
            if (currentDialogue == null) { return false; }
            return (GetChoiceCount() > 1 && GetNextSpeakerType() == SpeakerType.playerSpeaker);
        }

        public void NextWithID(string nodeID)
        {
            if (HasNext())
            {
                TriggerExitAction();
                currentNode = currentDialogue.GetNodeFromID(nodeID);
                TriggerEnterAction();
                if (HasNext()) // Skip re-showing the player choice
                {
                    Next();
                }
                else // Unless it's the last stem in the dialogue tree
                {
                    if (dialogueUpdated != null)
                    {
                        dialogueUpdated.Invoke();
                        TriggerEnterAction();
                    }
                }
            }
        }

        public void Next(bool withTriggers = true)
        {
            if (HasNext())
            {
                List<string> filteredDialogueOptions = FilterOnCondition(currentNode.GetChildren()).ToList();
                int nodeIndex = UnityEngine.Random.Range(0, filteredDialogueOptions.Count);
                if (withTriggers) { TriggerExitAction(); }
                currentNode = currentDialogue.GetNodeFromID(filteredDialogueOptions[nodeIndex]);
                if (withTriggers) { TriggerEnterAction(); }
                if (dialogueUpdated != null)
                {
                    dialogueUpdated.Invoke();
                }
            }
        }

        private void TriggerEnterAction()
        {
            TriggerAction(currentNode.GetOnEnterAction());
        }

        private void TriggerExitAction()
        {
            TriggerAction(currentNode.GetOnExitAction());
        }

        private void TriggerAction(string action)
        {
            if (currentNode != null && !string.IsNullOrWhiteSpace(action))
            {
                DialogueTrigger[] dialogueTriggers = currentConversant.GetComponents<DialogueTrigger>();
                foreach (DialogueTrigger dialogueTrigger in dialogueTriggers)
                {
                    dialogueTrigger.Trigger(action);
                }
            }
        }

        private IEnumerable<string> FilterOnCondition(List<string> dialogueNodeIDs)
        {
            foreach (string dialogueNodeID in dialogueNodeIDs)
            {
                if (currentDialogue.GetNodeFromID(dialogueNodeID).CheckCondition(GetEvaluators()))
                {
                    yield return dialogueNodeID;
                }
            }
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators()
        {
            return GetComponents<IPredicateEvaluator>().Concat(
                currentConversant.GetComponents<IPredicateEvaluator>());
        }
    }
}