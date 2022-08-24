using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Control;
using System;
using Frankie.Utils.UI;
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
        [Header("Parameters")]
        [SerializeField] float delayBetweenCharacters = 0.05f; // Seconds
        [SerializeField] bool reconfigureLayoutOnOptionSize = true;
        [SerializeField][Tooltip("[entry] Greater than this value will change options to vertical configuration")] int choiceNumberThresholdToReconfigureVertical = 2;
        [SerializeField][Tooltip("[char] Greater than this value will change options to vertical configuration")] int choiceLengthThresholdToReconfigureVertical = 10;

        // Option Field Configurables
        RectOffset optionPadding = default;
        float optionSpacing = 0f;
        TextAnchor optionChildAlignment = default;
        bool optionControlChildSize = true;
        bool optionUseChildScale = true;
        bool optionChildForceExpand = false;

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

        #region StandardMethods
        protected virtual void Awake()
        {
            controller = GameObject.FindGameObjectWithTag("DialogueController")?.GetComponent<DialogueController>();
            if (controller != null) { dialogueController = controller as DialogueController; }

            StoreOptionPanelConfigurables();
        }

        private void StoreOptionPanelConfigurables()
        {
            if (optionParent.TryGetComponent(out HorizontalLayoutGroup horizontalLayoutGroup))
            {
                optionPadding = horizontalLayoutGroup.padding;
                optionSpacing = horizontalLayoutGroup.spacing;
                optionChildAlignment = horizontalLayoutGroup.childAlignment;

                optionControlChildSize = horizontalLayoutGroup.childControlWidth;
                optionUseChildScale = horizontalLayoutGroup.childScaleWidth;
                optionChildForceExpand = horizontalLayoutGroup.childForceExpandWidth;
            }
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

        private void Start()
        {
            Setup(null);
        }

        private void OnDestroy()
        {
            // Called after disable unsubscribes, safety behavior for dialogue box killed without controller knowing
            if (dialogueController != null && dialogueController.HasDialogue())
            {
                dialogueController.EndConversation();
            }
        }

        public virtual void Setup(string optionText)
        {
            if (dialogueController != null && dialogueController.IsSimpleMessage())
            {
                AddText(dialogueController.GetSimpleMessage());
            }
            else
            {
                if (string.IsNullOrEmpty(optionText)) { return; }

                AddText(optionText);
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
            if (dialogueController == null && controller == null)
            {
                QueueDialogueCompletion();
            }
        }

        private void QueueDialogueCompletion()
        {
            destroyQueued = true;
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
        #endregion

        #region WritingFunctionality
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
        #endregion

        #region ChoiceFunctionality
        private void SetChoiceList()
        {
            int choiceIndex = 0;
            int maxChoiceLength = 0;
            foreach (DialogueNode choiceNode in dialogueController.GetChoices())
            {
                AddChoice(choiceNode, choiceIndex);
                maxChoiceLength = Mathf.Max(maxChoiceLength, choiceNode.GetText().Length);
                choiceIndex++;
            }

            ConfigureChoiceLayout(choiceIndex, maxChoiceLength);
        }

        protected void ConfigureChoiceLayout(int choiceCount, int maxChoiceLength)
        {
            if (!reconfigureLayoutOnOptionSize || choiceCount == 0) { return; }

            if (choiceCount > choiceNumberThresholdToReconfigureVertical || maxChoiceLength > choiceLengthThresholdToReconfigureVertical)
            {
                if (optionParent.TryGetComponent(out HorizontalLayoutGroup horizontalLayoutGroup))
                {
                    DestroyImmediate(horizontalLayoutGroup);
                }

                if (!optionParent.TryGetComponent(out VerticalLayoutGroup verticalLayoutGroup))
                {
                    verticalLayoutGroup = optionParent.gameObject.AddComponent(typeof(VerticalLayoutGroup)) as VerticalLayoutGroup;
                    verticalLayoutGroup.padding = optionPadding;
                    verticalLayoutGroup.spacing = optionSpacing;
                    verticalLayoutGroup.childAlignment = optionChildAlignment;
                    verticalLayoutGroup.childControlWidth = optionControlChildSize;
                    verticalLayoutGroup.childControlHeight = optionControlChildSize;
                    verticalLayoutGroup.childScaleWidth = optionUseChildScale;
                    verticalLayoutGroup.childScaleHeight = optionUseChildScale;
                    verticalLayoutGroup.childForceExpandWidth = optionChildForceExpand;
                    verticalLayoutGroup.childForceExpandHeight = optionChildForceExpand;
                }
            }
            else
            {
                if (optionParent.TryGetComponent(out VerticalLayoutGroup verticalLayoutGroup))
                {
                    DestroyImmediate(verticalLayoutGroup);
                }

                if (!optionParent.TryGetComponent(out HorizontalLayoutGroup horizontalLayoutGroup))
                {
                    horizontalLayoutGroup = optionParent.gameObject.AddComponent(typeof(HorizontalLayoutGroup)) as HorizontalLayoutGroup;
                    horizontalLayoutGroup.padding = optionPadding;
                    horizontalLayoutGroup.spacing = optionSpacing;
                    horizontalLayoutGroup.childAlignment = optionChildAlignment;
                    horizontalLayoutGroup.childControlWidth = optionControlChildSize;
                    horizontalLayoutGroup.childControlHeight = optionControlChildSize;
                    horizontalLayoutGroup.childScaleWidth = optionUseChildScale;
                    horizontalLayoutGroup.childScaleHeight = optionUseChildScale;
                    horizontalLayoutGroup.childForceExpandWidth = optionChildForceExpand;
                    horizontalLayoutGroup.childForceExpandHeight = optionChildForceExpand;
                }
            }
        }

        public void AddChoice(DialogueNode choiceNode, int choiceIndex = 0)
        {
            GameObject dialogueChoiceOptionObject = Instantiate(optionPrefab, optionParent);
            DialogueChoiceOption dialogueChoiceOption = dialogueChoiceOptionObject.GetComponent<DialogueChoiceOption>();
            dialogueChoiceOption.Setup(dialogueController, choiceNode);
            dialogueChoiceOption.SetChoiceOrder(choiceIndex);
            dialogueChoiceOption.SetText(choiceNode.GetText());
            dialogueChoiceOption.AddOnClickListener(delegate { Choose(choiceNode.name); });
            dialogueChoiceOption.gameObject.SetActive(false);

            QueueTextForPrinting(dialogueChoiceOption.gameObject, null, true);
        }

        private IEnumerator PrintChoices(GameObject choiceObject)
        {
            choiceObject.SetActive(true);
            yield break;
        }

        protected override bool Choose(string nodeID)
        {
            bool choose = PrepareChooseAction(PlayerInputType.Execute);
            if (choose)
            {
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);
                dialogueController.NextWithID(nodeID);
            }
            return choose;
        }

        protected override bool PrepareChooseAction(PlayerInputType playerInputType)
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
        #endregion

        #region InputHandling
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (HandleGlobalInputSpoofAndExit(playerInputType)) { return true; }

            if (playerInputType == PlayerInputType.Execute || playerInputType == PlayerInputType.Skip)
            {
                if (isWriting) { SkipToEndOfPage(); return true; }
                if (dialogueController != null && !dialogueController.IsSimpleMessage())
                {
                    return true;  // dialogue completion handled by dialogue controller
                }

                if (!IsChoiceAvailable())
                {
                    QueueDialogueCompletion(); // otherwise queue for deletion on click through
                    return true;
                }
            }

            return false;
        }

        private void HandleDialogueInput(PlayerInputType playerInputType)
        {
            PrepareChooseAction(playerInputType);
        }
        #endregion


    }
}