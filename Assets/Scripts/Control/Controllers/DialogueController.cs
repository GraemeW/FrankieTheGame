using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Core;
using Frankie.Stats;
using Frankie.Control;
using Frankie.World;
using Frankie.Utils;

namespace Frankie.Speech
{
    public class DialogueController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] private GameObject dialogueBoxPrefab;
        [SerializeField] private GameObject dialogueOptionBox;
        [SerializeField] private GameObject dialogueOptionBoxVertical;

        // State
        private PlayerInputType currentDirectionalInput = PlayerInputType.DefaultNone;
        
        private Dialogue currentDialogue;
        private DialogueNode currentNode;
        private AIConversant currentConversant;
        private DialogueNode highlightedNode;

        private bool isSimpleMessage = false;
        private string simpleMessage = "";
        private List<ChoiceActionPair> simpleChoices = new();
        private InteractionEvent onDestroyCallbackActions;

        private bool dialogueComplete = false;

        // Cached References
        private PlayerInput playerInput;
        private WorldCanvas worldCanvas;
        private PlayerStateMachine playerStateHandler;
        private Party party;

        // Events
        public event Action<PlayerInputType> globalInput;
        public event Action<PlayerInputType> dialogueInput;
        public event Action<DialogueNode> highlightedNodeChanged;
        public event Action triggerUIUpdates;
        public event Action<DialogueUpdateType, DialogueNode> dialogueUpdated;

        #region Static
        private const string _dialogueControllerTag = "DialogueController";

        public static DialogueController FindDialogueController()
        {
            var dialogueControllerGameObject = GameObject.FindGameObjectWithTag(_dialogueControllerTag);
            return dialogueControllerGameObject != null ? dialogueControllerGameObject.GetComponent<DialogueController>() : null;
        }

        private const int _choiceNumberThresholdToReconfigureVertical = 3;
        private const int _choiceLengthThresholdToReconfigureVertical = 10;
        public static int GetChoiceNumberThresholdToReconfigureVertical() => _choiceNumberThresholdToReconfigureVertical;
        public static int GetChoiceLengthThresholdToReconfigureVertical() => _choiceLengthThresholdToReconfigureVertical;
        #endregion

        #region UnityMethods
        private void Awake()
        {
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Navigate.canceled += _ => ParseDirectionalInput(Vector2.zero);
            
            playerInput.Menu.Execute.performed += _ => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += _ => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Menu.Option.performed += _ => HandleUserInput(PlayerInputType.Option);
            playerInput.Menu.Escape.performed += _ => HandleUserInput(PlayerInputType.Escape);
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
            if (globalInput != null || dialogueInput != null || dialogueUpdated != null) { return; }
            
            // Special handling for case of controller existence, no listeners, but dialogue not complete
            // Force exit into world in this case
            if (!dialogueComplete) { playerStateHandler.EnterWorld(); }
            Destroy(gameObject);
        }
        #endregion

        #region PublicGetters
        public bool HasDialogue() => currentDialogue != null;
        public bool IsSimpleMessage() => isSimpleMessage;
        public string GetSimpleMessage() => simpleMessage;
        public List<ChoiceActionPair> GetSimpleChoices() => simpleChoices;
        public bool IsActive() => (currentDialogue != null && currentNode != null);
        public SpeakerType GetCurrentSpeakerType() => currentNode.GetSpeakerType();
        public string GetCurrentSpeakerName() => currentNode.GetSpeakerName();
        public string GetText() => currentNode.GetText();

        public IEnumerable<DialogueNode> GetChoices()
        {
            return FilterOnCondition(currentNode.GetChildren()).Select(childID => currentDialogue.GetNodeFromID(childID));
        }

        public bool IsChoosing()
        {
            if (currentDialogue == null) { return false; }
            return (GetChoiceCount() > 1 && GetNextSpeakerType() == SpeakerType.PlayerSpeaker);
        }
        #endregion

        #region PublicSetters
        public void SetDestroyCallbackActions(InteractionEvent interactionEvent)
        {
            onDestroyCallbackActions = interactionEvent;
        }

        public void Setup(WorldCanvas setupWorldCanvas, PlayerStateMachine setupPlayerStateHandler, Party setupParty)
        {
            dialogueComplete = false;
            worldCanvas = setupWorldCanvas;
            playerStateHandler = setupPlayerStateHandler;
            party = setupParty;
        }

        private void SetupDialogueTriggers()
        { 
            if (currentConversant == null) { return; }
            
            // N.B.  Dialogue triggers need to live on same game object as conversant component
            foreach (DialogueTrigger dialogueTrigger in currentConversant.GetComponents<DialogueTrigger>())
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
            if (currentDialogue.skipRootNode) { Next(); }

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
        #endregion
        
        #region InteractionMethods
        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            if (!IStandardPlayerInputCaller.ParseDirectionalInput(directionalInput, currentDirectionalInput, out PlayerInputType newPlayerInputType)) { return; }
            currentDirectionalInput = newPlayerInputType;
            HandleUserInput(newPlayerInputType);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            if (!isSimpleMessage)
            {
                if (InteractWithChoices(playerInputType)) { return; }
                if (InteractWithNext(playerInputType)) { return; }
            }
            // ReSharper disable once RedundantJumpStatement
            if (InteractWithGlobals(playerInputType)) { return; }
        }

        private bool InteractWithGlobals(PlayerInputType playerInputType)
        {
            if (globalInput == null) { return false; }
            
            globalInput.Invoke(playerInputType); // handle text skip on dialogue box
            return true;
        }

        private bool InteractWithNext(PlayerInputType playerInputType)
        {
            if (IsChoosing() || playerInputType == PlayerInputType.DefaultNone) { return false; }
            if (triggerUIUpdates == null) { return false; }  // check if dialogue box can receive messages (toggled off during text-scans)
            if (playerInputType != PlayerInputType.Execute) { return false; }

            if (HasNext())
            {
                Next();
            }
            else
            {
                EndConversation();
            }
            return true;
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

            switch (playerInputType)
            {
                case PlayerInputType.Execute:
                {
                    dialogueInput?.Invoke(playerInputType);
                    NextWithID(highlightedNode.name);
                    highlightedNode = null;
                    return true;
                }
                case PlayerInputType.NavigateUp:
                case PlayerInputType.NavigateLeft:
                case PlayerInputType.NavigateRight:
                case PlayerInputType.NavigateDown:
                {
                    List<DialogueNode> currentOptions = GetChoices().ToList();

                    if (!currentOptions.Contains(highlightedNode))
                    {
                        SetHighlightedNodeToDefault(playerInputType);
                    }
                    else
                    {
                        HighlightNextNode(currentOptions, playerInputType);
                    }
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region PrivateMethods
        private string GetPlayerName() => party.GetPartyLeaderName();
        private SpeakerType GetNextSpeakerType() => currentDialogue.GetNodeFromID(currentNode.GetChildren()[0]).GetSpeakerType();
        private int GetChoiceCount() => FilterOnCondition(currentNode.GetChildren()).Count();
        private bool HasNext() => currentDialogue != null && FilterOnCondition(currentNode.GetChildren()).Any();
        
        private void Next()
        {
            if (!HasNext()) { return; }
            
            List<string> filteredDialogueOptions = FilterOnCondition(currentNode.GetChildren()).ToList();
            int nodeIndex = UnityEngine.Random.Range(0, filteredDialogueOptions.Count);
            SetCurrentNode(currentDialogue.GetNodeFromID(filteredDialogueOptions[nodeIndex]));
        }

        private GameObject ReckonDialogueOptionBox(List<ChoiceActionPair> choiceActionPairs)
        {
            if (choiceActionPairs.Count >= GetChoiceNumberThresholdToReconfigureVertical()) { return dialogueOptionBoxVertical; }
            return choiceActionPairs.Any(choiceActionPair => choiceActionPair.choice.Length >= GetChoiceLengthThresholdToReconfigureVertical()) ? dialogueOptionBoxVertical : dialogueOptionBox;
        }

        private void SetHighlightedNodeToDefault(PlayerInputType playerInputType)
        {
            if (playerInputType != PlayerInputType.Execute) { return; }
            
            highlightedNode = GetChoices().FirstOrDefault();
            highlightedNodeChanged?.Invoke(highlightedNode);
        }

        private void HighlightNextNode(List<DialogueNode> currentOptions, PlayerInputType playerInputType)
        {
            int choiceIndex = currentOptions.IndexOf(highlightedNode);
            switch (playerInputType)
            {
                case PlayerInputType.NavigateRight:
                case PlayerInputType.NavigateDown:
                {
                    if (choiceIndex + 1 >= currentOptions.Count) { choiceIndex = 0; }
                    else { choiceIndex++; }

                    break;
                }
                case PlayerInputType.NavigateUp:
                case PlayerInputType.NavigateLeft:
                {
                    if (choiceIndex <= 0) { choiceIndex = currentOptions.Count - 1; }
                    else { choiceIndex--; }

                    break;
                }
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
                DialogueNode dialogueNode = currentDialogue.GetNodeFromID(dialogueNodeID);
                if (dialogueNode == null) { continue; }
                
                if (dialogueNode.CheckCondition(GetEvaluators()))
                {
                    yield return dialogueNodeID;
                }
            }
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators()
        {
            // Evaluator locations . . . 
            // A. Player -> 
            //     1.  PlayerController
            //     2.  Party (childed to player controller)
            // B. AI conversant -- childed to character;  Grab Parent & GetComponentsInChildren traverses both the parent & children

            var predicateEvaluators = playerStateHandler.GetComponentsInChildren<IPredicateEvaluator>().Concat( // A
                currentConversant.transform.parent.gameObject.GetComponentsInChildren<IPredicateEvaluator>()); // B

            return predicateEvaluators;
        }
        #endregion

        #region Interfaces
        public void VerifyUnique()
        {
            var dialogueControllers = FindObjectsByType<DialogueController>(FindObjectsSortMode.None);
            if (dialogueControllers.Length > 1)
            {
                Destroy(gameObject);
            }
        }
        #endregion
    }
}
