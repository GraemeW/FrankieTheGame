using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Control;
using Frankie.Utils.UI;

namespace Frankie.Speech.UI
{
    public class DialogueBox : UIBox
    {
        // Tunables
        [Header("Links And Prefabs")]
        [SerializeField] protected Transform dialogueParent;
        [SerializeField] private GameObject simpleTextPrefab;
        [SerializeField] private GameObject speechTextPrefab;
        [Header("Parameters")]
        [SerializeField] private float delayBetweenCharacters = 0.05f; // Seconds
        [SerializeField] private bool reconfigureLayoutOnOptionSize = true;

        // Option Field Configurables
        private RectOffset optionPadding;
        private float optionSpacing;
        private TextAnchor optionChildAlignment;
        private bool optionControlChildSize = true;
        private bool optionUseChildScale = true;
        private bool optionChildForceExpand;

        // State -- Toggles
        private bool isWriting = false;
        private bool interruptWriting = false;
        private bool queuePageClear = false;
        private Coroutine activeTextScan;
        private readonly Queue<ReceptacleTextPair> printQueue = new();
        private List<GameObject> printedJobs = new();

        // Cached References
        protected DialogueController dialogueController;

        #region DataStructures
        private struct ReceptacleTextPair
        {
            public GameObject receptacle;
            public string text;
            public bool isChoice;
        }
        #endregion
        
        #region UnityMethods
        protected virtual void Awake()
        {
            dialogueController = DialogueController.FindDialogueController();
            controller = dialogueController;
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
            if (activeTextScan != null) { StopCoroutine(activeTextScan); }
            base.OnDisable();
        }

        private void Start()
        {
            Setup(null);
        }

        private void OnDestroy()
        {
            // Called after disable unsubscribes, safety behaviour for dialogue box killed without controller knowing
            if (dialogueController != null && dialogueController.HasDialogue())
            {
                dialogueController.EndConversation();
            }
        }
        #endregion

        #region SetupUpdateMethods
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

            OnUIBoxModified(UIBoxModifiedType.WritingStateChanged, enable);
        }

        private void SetText()
        {
            if (dialogueController.GetCurrentSpeakerType() == SpeakerType.PlayerSpeaker || dialogueController.GetCurrentSpeakerType() == SpeakerType.AISpeaker)
            {
                AddText(dialogueController.GetCurrentSpeakerName() + ":");
                AddSpeech(dialogueController.GetText());
            }
            else if (dialogueController.GetCurrentSpeakerType() == SpeakerType.NarratorDirection)
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
            var receptacleTextPair = new ReceptacleTextPair
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
            else if (receptacleTextPair.isChoice)
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
            OnUIBoxModified(UIBoxModifiedType.WritingStateChanged, false); // override printing to false, since not really printing -- wait for user input for next step

            queuePageClear = true;
            yield break;
        }

        private IEnumerator PrintText(ReceptacleTextPair receptacleTextPair)
        {
            if (string.IsNullOrWhiteSpace(receptacleTextPair.text)) { yield break; }
            receptacleTextPair.receptacle.SetActive(true);

            SetBusyWriting(true);
            SimpleTextLink simpleTextLink = receptacleTextPair.receptacle.GetComponent<SimpleTextLink>();
            if (simpleTextLink == null) { yield break; }
            string fullText = UnescapeText(receptacleTextPair.text);
            if (string.IsNullOrEmpty(fullText)) { yield break; }

            int letterIndex = 0;
            string textFragment = "";
            while (letterIndex < fullText.Length - 1)
            {
                if (interruptWriting) { break; }
                textFragment += fullText[letterIndex];
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

            if (choiceCount > DialogueController.GetChoiceNumberThresholdToReconfigureVertical() || maxChoiceLength > DialogueController.GetChoiceLengthThresholdToReconfigureVertical())
            {
                if (optionParent.TryGetComponent(out HorizontalLayoutGroup horizontalLayoutGroup))
                {
                    DestroyImmediate(horizontalLayoutGroup);
                }

                if (!optionParent.TryGetComponent(out VerticalLayoutGroup verticalLayoutGroup))
                {
                    verticalLayoutGroup = optionParent.gameObject.AddComponent(typeof(VerticalLayoutGroup)) as VerticalLayoutGroup;
                    if (verticalLayoutGroup == null) { return; }
                    
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
                    if (horizontalLayoutGroup == null) { return; }
                    
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
            GameObject dialogueChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
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
                OnUIBoxModified(UIBoxModifiedType.ItemSelected, true);
                dialogueController.NextWithID(nodeID);
            }
            return choose;
        }

        protected override bool PrepareChooseAction(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.Execute)
            {
                if (!isWriting) { return true; }
                
                if (activeTextScan != null) { StopCoroutine(activeTextScan); }
                SetBusyWriting(false);
                return true;
            }
            return false;
        }
        #endregion
        
        #region StringPreParse
        private static string UnescapeText(string inputString)
        {
            if (string.IsNullOrEmpty(inputString)) { return inputString; }

            var stringBuilder = new System.Text.StringBuilder(inputString.Length);
            int i = 0;
            while (i < inputString.Length)
            {
                char c = inputString[i];

                if (c != '\\' || i == inputString.Length - 1)
                {
                    stringBuilder.Append(c);
                    i++;
                    continue;
                }
                
                char next = inputString[i + 1];
                switch (next)
                {
                    case 'n': stringBuilder.Append('\n'); i += 2; break;
                    case 't': stringBuilder.Append('\t'); i += 2; break;
                    case 'r': stringBuilder.Append('\r'); i += 2; break;
                    case '\\': stringBuilder.Append('\\'); i += 2; break;
                    case '"': stringBuilder.Append('"'); i += 2; break;
                    case '\'': stringBuilder.Append('\''); i += 2; break;
                    case '0': stringBuilder.Append('\0'); i += 2; break;
                    case 'a': stringBuilder.Append('\a'); i += 2; break;
                    case 'b': stringBuilder.Append('\b'); i += 2; break;
                    case 'f': stringBuilder.Append('\f'); i += 2; break;
                    case 'v': stringBuilder.Append('\v'); i += 2; break;
                    case 'u':
                        // \uXXXX - exactly 4 hex digits required.
                        if (i + 5 < inputString.Length + 1 && i + 6 <= inputString.Length &&
                            TryParseHex(inputString, i + 2, 4, out int codeUnit))
                        {
                            stringBuilder.Append((char)codeUnit);
                            i += 6;
                        }
                        else
                        {
                            stringBuilder.Append(c);
                            i++;
                        }
                        break;
                    case 'x':
                        // \xH, \xHH, \xHHH, or \xHHHH - variable length (1-4 hex digits), greedy.
                        int hexLen = 0;
                        while (hexLen < 4 && i + 2 + hexLen < inputString.Length &&
                               Uri.IsHexDigit(inputString[i + 2 + hexLen]))
                        {
                            hexLen++;
                        }
                        if (hexLen > 0 && TryParseHex(inputString, i + 2, hexLen, out int hexVal))
                        {
                            stringBuilder.Append((char)hexVal);
                            i += 2 + hexLen;
                        }
                        else
                        {
                            stringBuilder.Append(c);
                            i++;
                        }
                        break;
                    default:
                        // Unrecognized escape (e.g. "\q") - pass through
                        stringBuilder.Append(c);
                        stringBuilder.Append(next);
                        i += 2;
                        break;
                }
            }
            return stringBuilder.ToString();
        }

        private static bool TryParseHex(string inputString, int start, int length, out int value)
        {
            value = 0;
            if (start + length > inputString.Length) { return false; }
            for (int k = 0; k < length; k++)
            {
                if (!Uri.IsHexDigit(inputString[start + k])) { return false; }
            }
            return int.TryParse( inputString.Substring(start, length), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value);
        }
        #endregion

        #region InputHandling
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (HandleGlobalInputSpoofAndExit(playerInputType)) { return true; }

            if (playerInputType == PlayerInputType.Execute)
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
