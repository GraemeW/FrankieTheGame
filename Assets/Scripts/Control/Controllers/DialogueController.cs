using Frankie.Core;
using Frankie.Stats;
using Frankie.Control;
using Frankie.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Speech
{
    public class DialogueController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] GameObject dialogueBoxPrefab = null;
        [SerializeField] GameObject dialogueOptionBox = null;
        [SerializeField] GameObject dialogueOptionBoxVertical = null;

        // State
        Dialogue currentDialogue = null;
        DialogueNode currentNode = null;
        AIConversant currentConversant = null;
        DialogueNode highlightedNode = null;

        bool isSimpleMessage = false;
        string simpleMessage = "";
        List<ChoiceActionPair> simpleChoices = new List<ChoiceActionPair>();
        InteractionEvent onDestroyCallbackActions = null;

        bool dialogueComplete = false;

        // Cached References
        PlayerInput playerInput = null;
        WorldCanvas worldCanvas = null;
        PlayerStateMachine playerStateHandler = null;
        Party party = null;

        // Events
        public event Action<PlayerInputType> globalInput;
        public event Action<PlayerInputType> dialogueInput;
        public event Action<DialogueNode> highlightedNodeChanged;
        public event Action triggerUIUpdates;
        public event Action<DialogueUpdateType, DialogueNode> dialogueUpdated;

        // Static
        static int choiceNumberThresholdToReconfigureVertical = 3;
        static int choiceLengthThresholdToReconfigureVertical = 10;
        public static int GetChoiceNumberThresholdToReconfigureVertical() => choiceNumberThresholdToReconfigureVertical;
        public static int GetChoiceLengthThresholdToReconfigureVertical() => choiceLengthThresholdToReconfigureVertical;

        // Interaction
        #region UnityMethods
        private void Awake()
        {
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Execute.performed += context => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += context => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Menu.Option.performed += context => HandleUserInput(PlayerInputType.Option);
            playerInput.Menu.Skip.performed += context => HandleUserInput(PlayerInputType.Skip);
        }

        private void OnEnable()
        {
            playerInput.Menu.Enable();
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
        }

        private void OnDestroy()
        {
            onDestroyCallbackActions?.Invoke(playerStateHandler);
        }

        private void Update()
        {
            KillControllerForNoReceivers();
        }

        private void KillControllerForNoReceivers()
        {
            if (globalInput == null && dialogueInput == null && dialogueUpdated == null)
            {
                if (!dialogueComplete) { playerStateHandler.EnterWorld(); }
                // Special handling for case of controller existence, no listeners, but dialogue not complete
                // Force exit into world in this case
                Destroy(gameObject);
            }
        }
        #endregion

        #region PublicGetters
        public bool HasDialogue() => currentDialogue != null;
        public bool IsSimpleMessage() => isSimpleMessage;
        public string GetSimpleMessage() => simpleMessage;
        public List<ChoiceActionPair> GetSimpleChoices() => simpleChoices;
        public string GetPlayerName() => party.GetPartyLeaderName();
        public bool IsActive() => (currentDialogue != null && currentNode != null);
        public SpeakerType GetCurrentSpeakerType() => currentNode.GetSpeakerType();
        public SpeakerType GetNextSpeakerType() => currentDialogue.GetNodeFromID(currentNode.GetChildren()[0]).GetSpeakerType();
        public int GetChoiceCount() => FilterOnCondition(currentNode.GetChildren()).Count();
        public string GetCurrentSpeakerName() => currentNode.GetSpeakerName();
        public string GetText() => currentNode.GetText();

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
        #endregion

        #region PublicSetters
        public void SetDestroyCallbackActions(InteractionEvent interactionEvent)
        {
            onDestroyCallbackActions = interactionEvent;
        }

        public void Setup(WorldCanvas worldCanvas, PlayerStateMachine playerStateHandler, Party party)
        {
            dialogueComplete = false;
            this.worldCanvas = worldCanvas;
            this.playerStateHandler = playerStateHandler;
            this.party = party;
        }

        private void SetupDialogueTriggers()
        {
            DialogueTrigger[] dialogueTriggers = currentConversant.GetComponents<DialogueTrigger>();  // N.B.  Dialogue triggers need to live on same game object as conversant component
            foreach (DialogueTrigger dialogueTrigger in dialogueTriggers)
            {
                dialogueTrigger.Setup(this, playerStateHandler);
            }
        }
        #endregion

        #region PublicUtility
        public void InitiateConversation(AIConversant newConversant, Dialogue newDialogue)
        {
            isSimpleMessage = false;
            currentConversant = newConversant;
            currentDialogue = newDialogue;
            currentDialogue.OverrideSpeakerNames(GetPlayerName());

            SetupDialogueTriggers();

            currentNode = currentDialogue.GetRootNode();
            // Call without announcing, dialogue not (officially) existing
            // Note:  No triggers on root node entry, but on dialogue entry
            if (currentDialogue.skipRootNode) { Next(false); }

            Instantiate(dialogueBoxPrefab, worldCanvas.transform);

            dialogueUpdated?.Invoke(DialogueUpdateType.DialogueInitiated, null);
            triggerUIUpdates?.Invoke();
        }

        public void InitiateSimpleMessage(string message)
        {
            isSimpleMessage = true;
            Instantiate(dialogueBoxPrefab, worldCanvas.transform);
            simpleMessage = message;
        }

        public void InitiateSimpleOption(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            isSimpleMessage = true;
            Instantiate(ReckonDialogueOptionBox(choiceActionPairs), worldCanvas.transform);

            simpleMessage = message;
            simpleChoices = choiceActionPairs;
        }

        private GameObject ReckonDialogueOptionBox(List<ChoiceActionPair> choiceActionPairs)
        {
            if (choiceActionPairs.Count >= DialogueController.GetChoiceNumberThresholdToReconfigureVertical()) { return dialogueOptionBoxVertical; }

            foreach (ChoiceActionPair choiceActionPair in choiceActionPairs)
            {
                if (choiceActionPair.choice.Length >= DialogueController.GetChoiceLengthThresholdToReconfigureVertical())
                {
                    return dialogueOptionBoxVertical;
                }
            }
            return dialogueOptionBox;
        }

        public void EndConversation()
        {
            currentDialogue = null;
            SetCurrentNode(null);
            triggerUIUpdates?.Invoke();
            playerStateHandler.EnterWorld();
            currentConversant = null;
            dialogueComplete = true;

            dialogueUpdated?.Invoke(DialogueUpdateType.DialogueComplete, null);
        }

        public void NextWithID(string nodeID)
        {
            if (HasNext())
            {
                SetCurrentNode(currentDialogue.GetNodeFromID(nodeID));
            }
        }

        public void Next(bool withTriggers = true)
        {
            if (HasNext())
            {
                List<string> filteredDialogueOptions = FilterOnCondition(currentNode.GetChildren()).ToList();
                int nodeIndex = UnityEngine.Random.Range(0, filteredDialogueOptions.Count);
                SetCurrentNode(currentDialogue.GetNodeFromID(filteredDialogueOptions[nodeIndex]));
            }
        }
        #endregion

        #region PrivateMethods
        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            PlayerInputType playerInputType = this.NavigationVectorToInputType(directionalInput);
            HandleUserInput(playerInputType);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            if (!isSimpleMessage)
            {
                if (InteractWithChoices(playerInputType)) { return; }
                if (InteractWithNext(playerInputType)) { return; }
            }
            if (InteractWithGlobals(playerInputType)) { return; }
        }

        private bool InteractWithGlobals(PlayerInputType playerInputType)
        {
            if (globalInput != null)
            {
                globalInput.Invoke(playerInputType); // handle text skip on dialogue box
                return true;
            }
            return false;
        }

        private bool InteractWithNext(PlayerInputType playerInputType)
        {
            if (IsChoosing() || playerInputType == PlayerInputType.DefaultNone) { return false; }
            if (triggerUIUpdates == null) { return false; }  // check if dialogue box can receive messages (toggled off during text-scans)

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

            if (playerInputType == PlayerInputType.Skip || playerInputType == PlayerInputType.Cancel)
            {
                if (!HasNext())
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
            if (triggerUIUpdates == null) { return false; }  // check if dialogue box can receive messages (toggled off during text-scans)

            if (highlightedNode == null)
            {
                SetHighlightedNodeToDefault(playerInputType);
                return true;
            }

            if (playerInputType == PlayerInputType.Execute)
            {
                dialogueInput?.Invoke(playerInputType);
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
                    SetHighlightedNodeToDefault(playerInputType);
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

        private void SetHighlightedNodeToDefault(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.Execute)
            {
                highlightedNode = GetChoices().FirstOrDefault();
                highlightedNodeChanged?.Invoke(highlightedNode);
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
            highlightedNodeChanged?.Invoke(highlightedNode);
        }

        private void SetCurrentNode(DialogueNode dialogueNode, bool withTriggers = true)
        {
            if (currentNode == dialogueNode) { return; }

            if (withTriggers) { dialogueUpdated?.Invoke(DialogueUpdateType.DialogueNodeExit, currentNode); }

            currentNode = dialogueNode;

            if (withTriggers) { dialogueUpdated?.Invoke(DialogueUpdateType.DialogueNodeEntry, currentNode); }
            triggerUIUpdates?.Invoke();
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
            // Evaluator locations . . . 
            // A) Player -> 
            //     1.  PlayerController
            //     2.  Party (childed to player controller)
            // B) AI conversant -- childed to character;  Grab Parent & GetComponentsInChildren traverses both the parent & children

            var predicateEvaluators = playerStateHandler.GetComponentsInChildren<IPredicateEvaluator>().Concat( // A
                currentConversant.transform.parent.gameObject.GetComponentsInChildren<IPredicateEvaluator>()); // B

            return predicateEvaluators;
        }
        #endregion

        #region Interfaces
        public void VerifyUnique()
        {
            DialogueController[] dialogueControllers = FindObjectsOfType<DialogueController>();
            if (dialogueControllers.Length > 1)
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