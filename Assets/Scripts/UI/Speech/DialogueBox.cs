using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Control;

namespace Frankie.Speech.UI
{
    public class DialogueBox : MonoBehaviour, IGlobalInputReceiver
    {
        // Tunables
        [Header("Links And Prefabs")]
        [SerializeField] protected Transform dialogueParent = null;
        [SerializeField] GameObject simpleTextPrefab = null;
        [SerializeField] GameObject speechTextPrefab = null;
        [SerializeField] protected Transform optionParent = null;
        [SerializeField] GameObject optionPrefab = null;
        [Header("Parameters")]
        [SerializeField] protected bool handleGlobalInput = true;
        [SerializeField] float delayToDestroyWindow = 0.1f; // Seconds
        [SerializeField] float delayBetweenCharacters = 0.05f; // Seconds
        [Header("Choices")]
        [SerializeField] protected KeyCode choiceInteractKey = KeyCode.E;

        // State -- Toggles
        bool isWriting = false;
        bool interruptWriting = false;
        bool queuePageClear = false;
        bool destroyQueued = false;

        // State -- Stored references
        Coroutine activeTextScan = null;
        Queue<ReceptacleTextPair> printQueue = new Queue<ReceptacleTextPair>();
        List<GameObject> printedJobs = new List<GameObject>();
        List<CallbackMessagePair> disableCallbacks = new List<CallbackMessagePair>();
        IStandardPlayerInputCaller alternateController = null;

        // Cached References
        DialogueController dialogueController = null;

        // Structures
        private struct ReceptacleTextPair
        {
            public GameObject receptacle;
            public string text;
            public bool isChoice;
        }

        private struct CallbackMessagePair
        {
            public IDialogueBoxCallbackReceiver receiver;
            public string message;
        }

        protected virtual void Awake()
        {
            GameObject dialogueControllerObject = GameObject.FindGameObjectWithTag("DialogueController");
            if (dialogueControllerObject != null) { dialogueController = dialogueControllerObject.GetComponent<DialogueController>(); }
            destroyQueued = false;
        }

        protected virtual void OnEnable()
        {
            if (dialogueController != null && handleGlobalInput)
            {
                if (handleGlobalInput) { dialogueController.globalInput += HandleGlobalInput; }
                dialogueController.dialogueInput += HandleDialogueInput;
                dialogueController.dialogueUpdated += UpdateUI;
            }
            if (alternateController != null && handleGlobalInput)
            {
                alternateController.globalInput += HandleGlobalInput;
            }
        }

        protected virtual void OnDisable()
        {
            if (dialogueController != null)
            {
                if (handleGlobalInput) { dialogueController.globalInput -= HandleGlobalInput; }
                dialogueController.dialogueInput -= HandleDialogueInput;
                dialogueController.dialogueUpdated -= UpdateUI;
            }
            if (alternateController != null && handleGlobalInput)
            {
                alternateController.globalInput -= HandleGlobalInput;
            }

            foreach (CallbackMessagePair callbackMessagePair in disableCallbacks)
            {
                callbackMessagePair.receiver.HandleDialogueCallback(this, callbackMessagePair.message);
            }
        }

        private void Start()
        {
            SetupSimpleMessage();
        }

        private void SetupSimpleMessage()
        {
            if (dialogueController != null && dialogueController.IsSimpleMessage())
            {
                AddText(dialogueController.GetSimpleMessage());
            }
        }

        protected virtual void Update()
        {
            if (destroyQueued) { return; }

            if (!isWriting && printQueue.Count != 0)
            {
                activeTextScan = StartCoroutine(TextScan(printQueue.Dequeue()));
            }
        }

        public void SetGlobalCallbacks(IStandardPlayerInputCaller globalCallbackSender)
        {
            handleGlobalInput = true;
            globalCallbackSender.globalInput += HandleGlobalInput;
        }

        public void SetDisableCallback(IDialogueBoxCallbackReceiver callbackReceiver, DialogueBox dialogueBox, string callbackMessage)
        {
            CallbackMessagePair callbackMessagePair = new CallbackMessagePair
            {
                receiver = callbackReceiver,
                message = callbackMessage
            };
            disableCallbacks.Add(callbackMessagePair);
        }

        private void UpdateUI()
        {
            KillDialogueForNoControllers();
            if (!dialogueController.IsActive()) { QueueDialogueCompletion(); return; }

            ClearOldDialogue();
            SetText();
            if (dialogueController.IsChoosing())
            {
                SetChoiceList();
            }
        }

        private void KillDialogueForNoControllers()
        {
            if (dialogueController == null && alternateController == null)
            {
                QueueDialogueCompletion();
            }
        }

        private void QueueDialogueCompletion()
        {
            if (!destroyQueued)
            {
                destroyQueued = true;
                Destroy(gameObject, delayToDestroyWindow);
            }
        }

        private void SetBusyWriting(bool enable)
        {
            if (enable)
            {
                isWriting = enable;
                if (dialogueController != null) { dialogueController.dialogueUpdated -= UpdateUI; } // unsubscribe from updates - prevent dialogue controller moving on while writing
            }
            else
            {
                isWriting = enable;
                if (dialogueController != null) { dialogueController.dialogueUpdated += UpdateUI; }
            }
        }

        private void SetText()
        {
            if (dialogueController.GetCurrentSpeakerType() == SpeakerType.playerSpeaker || dialogueController.GetCurrentSpeakerType() == SpeakerType.aiSpeaker)
            {
                AddText(dialogueController.GetCurrentSpeakerName() + ":");
                AddSpeech(dialogueController.GetText());
            }
            else if (dialogueController.GetCurrentSpeakerType() == SpeakerType.narratorDirection)
            {
                AddSpeech(dialogueController.GetText());
            }
        }

        private void SetChoiceList()
        {
            int choiceIndex = 0;
            foreach (DialogueNode choiceNode in dialogueController.GetChoices())
            {
                AddChoice(choiceNode, choiceIndex);
                choiceIndex++;
            }
        }

        private void ClearOldDialogue()
        {
            ClearPrintedJobs();
            foreach (Transform child in dialogueParent)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in optionParent)
            {
                child.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(child.gameObject);
            }
        }

        public void AddText(string text)
        {
            GameObject textObject = Instantiate(simpleTextPrefab, dialogueParent);
            textObject.SetActive(false);
            QueueTextForPrinting(textObject, text, false);
        }

        public void AddSpeech(string text)
        {
            GameObject textObject = Instantiate(speechTextPrefab, dialogueParent);
            textObject.SetActive(false);
            QueueTextForPrinting(textObject, text, false);
        }

        public void AddPageBreak()
        {
            QueueTextForPrinting(null, "BREAK", false);
        }

        public void AddChoice(DialogueNode choiceNode, int choiceIndex = 0)
        {
            GameObject choiceObject = Instantiate(optionPrefab, optionParent);
            DialogueChoiceOption dialogueChoiceOption = choiceObject.GetComponent<DialogueChoiceOption>();
            dialogueChoiceOption.Setup(dialogueController, choiceNode);
            dialogueChoiceOption.SetChoiceOrder(choiceIndex);
            dialogueChoiceOption.SetText(choiceNode.GetText());
            choiceObject.GetComponent<Button>().onClick.AddListener(delegate { Choose(choiceNode.name); });
            choiceObject.SetActive(false);

            QueueTextForPrinting(choiceObject, null, true);
        }

        private void QueueTextForPrinting(GameObject textObject, string text, bool isChoice)
        {
            ReceptacleTextPair receptacleTextPair = new ReceptacleTextPair
            {
                receptacle = textObject,
                text = text,
                isChoice = isChoice
            };
            printQueue.Enqueue(receptacleTextPair);
        }

        private IEnumerator TextScan(ReceptacleTextPair receptacleTextPair)
        {
            if (receptacleTextPair.receptacle == null)
            {
                yield return PrintPageBreak();
            }
            else if (receptacleTextPair.isChoice == true)
            {
                yield return PrintChoices(receptacleTextPair.receptacle);
            }
            else
            {
                yield return PrintText(receptacleTextPair);
            }
        }

        private IEnumerator PrintPageBreak()
        {
            SetBusyWriting(true);
            queuePageClear = true;
            yield break;
        }

        private IEnumerator PrintChoices(GameObject choiceObject)
        {
            choiceObject.SetActive(true);
            yield break;
        }

        private IEnumerator PrintText(ReceptacleTextPair receptacleTextPair)
        {
            if (string.IsNullOrWhiteSpace(receptacleTextPair.text)) { yield break; }
            receptacleTextPair.receptacle.SetActive(true);

            SetBusyWriting(true);
            SimpleTextLink simpleTextLink = receptacleTextPair.receptacle.GetComponent<SimpleTextLink>();

            int letterIndex = 0;
            string textFragment = "";
            while (letterIndex < receptacleTextPair.text.Length - 1)
            {
                if (interruptWriting) { break; }
                textFragment += receptacleTextPair.text[letterIndex];
                if (simpleTextLink == null) { break; }
                simpleTextLink.Setup(textFragment);
                letterIndex++;
                yield return new WaitForSeconds(delayBetweenCharacters);
            }
            if (simpleTextLink != null) { simpleTextLink.Setup(receptacleTextPair.text); }
            printedJobs.Add(receptacleTextPair.receptacle);
            SetBusyWriting(false);
            interruptWriting = false;
        }

        private void ClearPrintedJobs()
        {
            foreach (GameObject printedJob in printedJobs)
            {
                if (printedJob != null) { Destroy(printedJob); }
            }
            printedJobs = new List<GameObject>();
        }

        protected virtual bool IsChoiceAvailable()
        {
            // Used in alternate implementations
            return true;
        }

        protected virtual bool ShowCursorOnAnyInteraction(PlayerInputType playerInputType)
        {
            // Used in alternate implementations
            return true;
        }

        protected virtual bool MoveCursor(PlayerInputType playerInputType)
        {
            // Used in alternate implementations
            return true;
        }

        protected virtual bool Choose(string nodeID)
        {
            bool choose = PrepareChooseAction(PlayerInputType.Execute);
            if (choose) { dialogueController.NextWithID(nodeID); }
            return choose;
        }

        protected virtual bool PrepareChooseAction(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.Execute)
            {
                if (isWriting)
                {
                    if (activeTextScan != null) { StopCoroutine(activeTextScan); }
                    SetBusyWriting(false);
                }
                return true;
            }
            return false;
        }

        private void HandleDialogueInput(PlayerInputType playerInputType)
        {
            PrepareChooseAction(playerInputType);
        }

        public virtual void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (playerInputType == PlayerInputType.Execute)
            {
                if (isWriting) { SkipToEndOfPage(); return; }

                if (dialogueController != null && !dialogueController.IsSimpleMessage()) { return; } // dialogue completion handled by dialogue controller
                else
                {
                    QueueDialogueCompletion(); // otherwise queue for deletion on click through
                }
            }
        }

        private void SkipToEndOfPage()
        {
            interruptWriting = true;
            if (queuePageClear)
            {
                ClearPrintedJobs();
                SetBusyWriting(false);
                interruptWriting = false;
                queuePageClear = false;
            }
        }
    }
}