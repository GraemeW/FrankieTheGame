using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.Control;
using LowDefMustard.Utils;
using Frankie.Control;
using Frankie.Core;
using Frankie.Stats;
using Frankie.World;

namespace Frankie.Speech
{
    public class DialogueController : BaseController
    {
        // Tunables
        [Header("Controller Properties")]
        [SerializeField] private GameObject dialogueBoxPrefab;
        [SerializeField] private GameObject dialogueOptionBox;

        // State
        private ControllerInputType currentDirectionalInput = ControllerInputType.DefaultNone;
        
        private Dialogue currentDialogue;
        private DialogueNode currentNode;
        private AIConversant currentConversant;
        private DialogueNode highlightedNode;

        private bool isSimpleMessage = false;
        private string simpleMessage = "";
        private List<ChoiceActionPair> simpleChoices = new();
        private InteractionEvent onDestroyCallbackActions;
        private bool earlyExit = false;
        private bool dialogueComplete = false;

        // Cached References
        private PlayerInput playerInput;
        private WorldUIRoot worldUIRoot;
        private PlayerStateMachine playerStateMachine;
        private Party party;

        // Events
        private event Action<ControllerInputType> dialogueInput;
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

        #region ProtectedLifeCycle
        protected override bool HasAlternateReceiversActive() => dialogueUpdated != null;
        protected override bool ShouldDestroyForNoReceivers() => true;
        protected override void OnNoReceiversIdentified() => this.StandardOnNoReceiversIdentified();
        #endregion
        
        #region UnityMethods
        private void Awake()
        {
            playerInput = new PlayerInput();
            
            if (!VerifyUnique()) { return; }
            
            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Navigate.canceled += _ => ParseDirectionalInput(Vector2.zero);
            playerInput.Menu.Execute.performed += _ => HandleUserInput(ControllerInputType.Execute);
            playerInput.Menu.Cancel.performed += _ => HandleUserInput(ControllerInputType.Cancel);
            playerInput.Menu.Option.performed += _ => HandleUserInput(ControllerInputType.Option);
            playerInput.Menu.Escape.performed += _ => HandleUserInput(ControllerInputType.Escape);
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
            onDestroyCallbackActions?.Invoke(playerStateMachine);
        }

        protected override void LateUpdate()
        {
            if (earlyExit) { earlyExit = false; EndConversation(); }
            if (destroyQueued && !dialogueComplete) { playerStateMachine.EnterWorld(); }
            base.LateUpdate();
        }
        #endregion

        #region PublicGetters
        public bool IsSimpleMessage() => isSimpleMessage;
        public string GetSimpleMessage() => simpleMessage;
        public List<ChoiceActionPair> GetSimpleChoices() => simpleChoices;
        public bool IsActive() => (currentDialogue != null && currentNode != null);
        public SpeakerType GetCurrentSpeakerType() => currentNode != null ? currentNode.GetSpeakerType() : SpeakerType.NarratorDirection;
        public string GetCurrentSpeakerName() => currentNode != null ? currentNode.GetSpeakerName() : "";
        public string GetText() => currentNode != null ? currentNode.GetText() : "";

        public IEnumerable<DialogueNode> GetChoices() => GetNextNodes();

        public bool IsChoosing()
        {
            if (currentDialogue == null) { return false; }
            List<DialogueNode> nextNodes = GetNextNodes();
            return nextNodes.Count > 1 && nextNodes[0].GetSpeakerType() == SpeakerType.PlayerSpeaker;
        }
        #endregion

        #region PublicSetters

        public void SubscribeToDialogueInput(bool enable, Action<ControllerInputType> action)
        {
            dialogueInput -= action;
            if (enable) { dialogueInput += action; }
        }
        
        public void SetDestroyCallbackActions(InteractionEvent interactionEvent) => onDestroyCallbackActions = interactionEvent;

        public void Setup(WorldUIRoot setupWorldUIRoot, PlayerStateMachine setupPlayerStateMachine, Party setupParty)
        {
            dialogueComplete = false;
            worldUIRoot = setupWorldUIRoot;
            playerStateMachine = setupPlayerStateMachine;
            party = setupParty;
        }

        private void SetupDialogueTriggers()
        { 
            if (currentConversant == null) { return; }
            
            // N.B.  Dialogue triggers need to live on same game object as conversant component
            foreach (DialogueTrigger dialogueTrigger in currentConversant.GetComponents<DialogueTrigger>())
            {
                dialogueTrigger.Setup(this, playerStateMachine);
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

            List<DialogueNode> entryNodes = currentDialogue.ResolveEntry(GetEvaluators());
            if (entryNodes.Count == 0) { earlyExit = true; return; } // Defer EndConversation a frame to allow dialogue to open -> close
            
            SetCurrentNode(entryNodes[UnityEngine.Random.Range(0, entryNodes.Count)]);

            Instantiate(dialogueBoxPrefab, worldUIRoot.transform);
            dialogueUpdated?.Invoke(DialogueUpdateType.DialogueInitiated, null);
            triggerUIUpdates?.Invoke();
        }

        public void InitiateSimpleMessage(string message)
        {
            isSimpleMessage = true;
            Instantiate(dialogueBoxPrefab, worldUIRoot.transform);
            simpleMessage = message;
        }

        public void InitiateSimpleOption(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            isSimpleMessage = true;
            Instantiate(dialogueOptionBox, worldUIRoot.transform);
            simpleMessage = message;
            simpleChoices = choiceActionPairs;
        }
        
        public void EndConversation()
        {
            currentDialogue = null;
            SetCurrentNode(null);
            triggerUIUpdates?.Invoke();
            playerStateMachine.EnterWorld();
            currentConversant = null;
            dialogueComplete = true;

            dialogueUpdated?.Invoke(DialogueUpdateType.DialogueComplete, null);
            destroyQueued = true;
        }

        public void NextWithID(string nodeID)
        {
            if (!HasNext()) { return; }
            SetCurrentNode(currentDialogue.GetDialogueNodeFromID(nodeID));
        }
        #endregion
        
        #region InteractionMethods
        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            if (!BaseController.ParseDirectionalInput(directionalInput, currentDirectionalInput, out ControllerInputType newControllerInputType)) { return; }
            currentDirectionalInput = newControllerInputType;
            HandleUserInput(newControllerInputType);
        }
        
        private void TriggerDialogueInput(ControllerInputType controllerInputType)
        {
            if (dialogueInput == null) { return; }
            timeSinceLastPolled = 0f;
            dialogueInput.Invoke(controllerInputType);
        }

        private void HandleUserInput(ControllerInputType controllerInputType)
        {
            if (!isSimpleMessage)
            {
                if (InteractWithChoices(controllerInputType)) { return; }
                if (InteractWithNext(controllerInputType)) { return; }
            }
            if (InteractWithGlobals(controllerInputType)) { return; }
        }

        private bool InteractWithGlobals(ControllerInputType controllerInputType)
        {
            if (!HasGlobalInput()) { return false; }
            
            // handle text skip on dialogue box
            TriggerGlobalInput(controllerInputType);
            return true;
        }

        private bool InteractWithNext(ControllerInputType controllerInputType)
        {
            if (IsChoosing() || controllerInputType == ControllerInputType.DefaultNone) { return false; }
            if (triggerUIUpdates == null) { return false; }  // check if dialogue box can receive messages (toggled off during text-scans)
            if (controllerInputType != ControllerInputType.Execute) { return false; }

            if (HasNext())
            {
                Next();
            }
            else { EndConversation(); }
            return true;
        }

        private bool InteractWithChoices(ControllerInputType controllerInputType)
        {
            if (!IsChoosing() || controllerInputType == ControllerInputType.DefaultNone) { return false; }
            if (triggerUIUpdates == null) { return false; }  // check if dialogue box can receive messages (toggled off during text-scans)

            if (highlightedNode == null)
            {
                SetHighlightedNodeToDefault(controllerInputType);
                return true;
            }

            switch (controllerInputType)
            {
                case ControllerInputType.Execute:
                {
                    TriggerDialogueInput(controllerInputType);
                    NextWithID(highlightedNode.GetNodeID());
                    highlightedNode = null;
                    return true;
                }
                case ControllerInputType.NavigateUp:
                case ControllerInputType.NavigateLeft:
                case ControllerInputType.NavigateRight:
                case ControllerInputType.NavigateDown:
                {
                    List<DialogueNode> currentOptions = GetChoices().ToList();

                    if (!currentOptions.Contains(highlightedNode))
                    {
                        SetHighlightedNodeToDefault(controllerInputType);
                    }
                    else
                    {
                        HighlightNextNode(currentOptions, controllerInputType);
                    }
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region PrivateMethods
        private string GetPlayerName() => party.GetPartyLeaderName();
        private List<DialogueNode> GetNextNodes() => currentDialogue != null ? currentDialogue.ResolveChildren(currentNode, GetEvaluators()) : new List<DialogueNode>();
        private bool HasNext() => GetNextNodes().Count > 0;
        
        private void Next()
        {
            List<DialogueNode> nextNodes = GetNextNodes();
            if (nextNodes.Count == 0) { return; }
            
            SetCurrentNode(nextNodes[UnityEngine.Random.Range(0, nextNodes.Count)]);
        }

        private void SetHighlightedNodeToDefault(ControllerInputType controllerInputType)
        {
            if (controllerInputType != ControllerInputType.Execute) { return; }
            
            highlightedNode = GetChoices().FirstOrDefault();
            highlightedNodeChanged?.Invoke(highlightedNode);
        }

        private void HighlightNextNode(List<DialogueNode> currentOptions, ControllerInputType controllerInputType)
        {
            int choiceIndex = currentOptions.IndexOf(highlightedNode);
            switch (controllerInputType)
            {
                case ControllerInputType.NavigateRight:
                case ControllerInputType.NavigateDown:
                {
                    if (choiceIndex + 1 >= currentOptions.Count) { choiceIndex = 0; }
                    else { choiceIndex++; }

                    break;
                }
                case ControllerInputType.NavigateUp:
                case ControllerInputType.NavigateLeft:
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
            if (currentNode == null) { return; }
            
            if (withTriggers) { dialogueUpdated?.Invoke(DialogueUpdateType.DialogueNodeEntry, currentNode); }
            triggerUIUpdates?.Invoke();
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators()
        {
            // Evaluator locations . . . 
            // A. Player -> 
            //     1.  PlayerController
            //     2.  Party (childed to player controller)
            // B. AI conversant -- childed to character;  Grab Parent & GetComponentsInChildren traverses both the parent & children

            var predicateEvaluators = playerStateMachine.GetComponentsInChildren<IPredicateEvaluator>().Concat( // A
                currentConversant.transform.parent.gameObject.GetComponentsInChildren<IPredicateEvaluator>()); // B

            return predicateEvaluators;
        }
        #endregion
    }
}
