using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Control;
using System;
using Frankie.Utils;

namespace Frankie.Speech.UI
{
    public class DialogueBox : UIBox
    {
        // Tunables
        [Header("Links And Prefabs")]
        [SerializeField] protected Transform dialogueParent = null;
        [SerializeField] GameObject simpleTextPrefab = null;
        [SerializeField] GameObject speechTextPrefab = null;
        [SerializeField] protected Transform optionParent = null;
        [SerializeField] protected GameObject optionPrefab = null;
        [Header("Parameters")]
        [SerializeField] float delayBetweenCharacters = 0.05f; // Seconds

        // State -- Toggles
        bool isWriting = false;
        bool interruptWriting = false;
        bool queuePageClear = false;
        Coroutine activeTextScan = null;
        Queue<ReceptacleTextPair> printQueue = new Queue<ReceptacleTextPair>();
        List<GameObject> printedJobs = new List<GameObject>();

        // Cached References
        protected DialogueController dialogueController = null;

        // Structures
        private struct ReceptacleTextPair
        {
            public GameObject receptacle;
            public string text;
            public bool isChoice;
        }

        protected virtual void Awake()
        {
            GameObject dialogueControllerObject = GameObject.FindGameObjectWithTag("DialogueController");
            if (dialogueControllerObject != null) { controller = dialogueControllerObject.GetComponent<DialogueController>(); dialogueController = controller as DialogueController; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (dialogueController != null)
            {
                dialogueController.dialogueInput += HandleDialogueInput;
                dialogueController.triggerUIUpdates += UpdateUI;
            }

        }

        protected override void OnDisable()
        {
            if (dialogueController != null)
            {
                dialogueController.dialogueInput -= HandleDialogueInput;
                dialogueController.triggerUIUpdates -= UpdateUI;
            }
            base.OnDisable();
        }

        protected virtual void Start()
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

        protected virtual void UpdateUI()
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
            if (dialogueController == null && base.controller == null)
            {
                QueueDialogueCompletion();
            }
        }

        private void QueueDialogueCompletion()
        {
            destroyQueued = true;
        }

        private void SetBusyWriting(bool enable)
        {
            if (enable)
            {
                if (dialogueController != null) { dialogueController.triggerUIUpdates -= UpdateUI; } // unsubscribe from updates - prevent dialogue controller moving on while writing
            }
            else
            {
                if (dialogueController != null) { dialogueController.triggerUIUpdates += UpdateUI; }
            }
            isWriting = enable;

            OnUIBoxModified(UIBoxModifiedType.writingStateChanged, enable);
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

        protected void QueueTextForPrinting(GameObject textObject, string text, bool isChoice)
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
            OnUIBoxModified(UIBoxModifiedType.writingStateChanged, false); // override printing to false, since not really printing -- wait for user input for next step

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

        protected virtual bool Choose(string nodeID)
        {
            bool choose = PrepareChooseAction(PlayerInputType.Execute);
            if (choose)
            {
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);
                dialogueController.NextWithID(nodeID);
            }
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

        // Abstract Method Implementation
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (base.HandleGlobalInput(playerInputType)) { return true; } // Already handled

            if (playerInputType == PlayerInputType.Execute || playerInputType == PlayerInputType.Skip)
            {
                if (isWriting) { SkipToEndOfPage(); return true; }
                if (dialogueController != null)
                {
                    if (!dialogueController.IsSimpleMessage())
                    { 
                        return true;  // dialogue completion handled by dialogue controller
                    } 
                }

                if (!IsChoiceAvailable())
                {
                    QueueDialogueCompletion(); // otherwise queue for deletion on click through
                    return true;
                }
            }

            return false;
        }

        protected override bool IsChoiceAvailable()
        {
            // Used in alternate implementations
            return false;
        }

        protected override bool ShowCursorOnAnyInteraction(PlayerInputType playerInputType)
        {
            // No interactions available in simple dialogue box
            return false;
        }

        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            // No cursor available in simple dialogue box
            return false;
        }
    }
}