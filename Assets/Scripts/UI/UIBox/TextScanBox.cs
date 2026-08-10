using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LowDefMustard.Control;
using LowDefMustard.Utils;

namespace Frankie.Utils.UI
{
    public class TextScanBox : UIBox<UIBoxState>
    {
               // Tunables
        [Header("Links And Prefabs")]
        [SerializeField] protected Transform dialogueParent;
        [SerializeField] private GameObject simpleTextPrefab;
        [SerializeField] private GameObject speechTextPrefab;
        [Header("Parameters")]
        [SerializeField] private float initialInputDelay = 0.1f; // Seconds
        [SerializeField] private float delayBetweenCharacters = 0.05f; // Seconds

        // State
        private float timeSinceStart = 0f;
        protected bool isInitialInputBlocked = true;
        protected bool isWriting = false;
        private bool interruptWriting = false;
        private bool queuePageClear = false;
        private Coroutine activeTextScan;
        private readonly Queue<ReceptacleTextPair> printQueue = new();
        private List<GameObject> printedJobs = new();
        
        // UIBox Configuration
        protected override EnumLookup<UIBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var dialogueBoxConfiguration = new EnumLookup<UIBoxState,UIBoxStateBehaviour>();
            var defaultStateBehaviour = new UIBoxStateBehaviour( 
                prepareChooseAction: ImplementPrepareChooseAction,
                handleGlobalInput: ImplementHandleGlobalInput);
            dialogueBoxConfiguration.TrySet(UIBoxState.Default, defaultStateBehaviour);
            return dialogueBoxConfiguration;
        }

        #region DataStructures
        private struct ReceptacleTextPair
        {
            public GameObject receptacle;
            public string text;
            public bool isChoice;
        }
        #endregion
        
        #region UnityMethods
        protected override void DisableTriggered()
        {
            if (activeTextScan != null)
            {
                StopCoroutine(activeTextScan);
                SetBusyWriting(false);
                ClearOldDialogue();
            }
        }

        protected override void StartTriggered()
        {
            Setup(null);
        }
        #endregion

        #region SetupUpdateMethods
        public virtual void Setup(string text)
        {
            if (string.IsNullOrEmpty(text)) { return; }
            AddText(text);
        }

        protected virtual void Update()
        {
            if (destroyQueued) { return; }

            if (isInitialInputBlocked)
            {
                timeSinceStart += Time.deltaTime;
                if (timeSinceStart >= initialInputDelay) { isInitialInputBlocked = false; }
            }
            
            if (!isWriting && printQueue.Count != 0)
            {
                activeTextScan = StartCoroutine(TextScan(printQueue.Dequeue()));
            }
        }

        protected void SkipToEndOfPage()
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
        
        protected bool TryFastForwardActiveText()
        {
            if (!isWriting) { return false; }
            SkipToEndOfPage();
            return true;
        }
        #endregion

        #region WritingFunctionality
        private void SetBusyWriting(bool enable)
        {
            OnBusyWriting(enable);
            isWriting = enable;

            TriggerUIBoxModified(ReceiverModifiedType.WritingStateChanged, new ReceiverModifiedData(this, enable));
        }

        protected virtual void OnBusyWriting(bool enable) { }

        public void ClearOldDialogue()
        {
            ClearPrintedJobs();
            foreach (Transform child in dialogueParent)
            {
                Destroy(child.gameObject);
            }
            if (optionParent == null) { return; }
            
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
            TriggerUIBoxModified(ReceiverModifiedType.WritingStateChanged, new ReceiverModifiedData(this, false)); // override printing to false, since not really printing -- wait for user input for next step

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
        private IEnumerator PrintChoices(GameObject choiceObject)
        {
            choiceObject.SetActive(true);
            yield break;
        }

        private bool ImplementPrepareChooseAction(ControllerInputType controllerInputType)
        {
            if (controllerInputType != ControllerInputType.Execute) { return false; }
            TryFastForwardActiveText();
            return true;
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
        private bool ImplementHandleGlobalInput(ControllerInputType controllerInputType)
        {
            if (isInitialInputBlocked) { return false; }
            if (!handleGlobalInput) { return true; }
            if (TryEarlyExit(controllerInputType)) { return true; }

            if (controllerInputType != ControllerInputType.Execute) { return false; }
            
            if (isWriting) { SkipToEndOfPage(); return true; }
            if (!IsChoiceAvailable())
            {
                destroyQueued = true; // otherwise queue for deletion on click through
                return true;
            }
            return false;
        }
        #endregion
    }
}
