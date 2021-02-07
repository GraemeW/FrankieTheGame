using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Control;

namespace Frankie.Dialogue.UI
{
    public class DialogueBox : MonoBehaviour, IGlobalInput
    {
        // Tunables
        [Header("Links And Prefabs")]
        [SerializeField] protected Transform dialogueParent = null;
        [SerializeField] GameObject simpleTextPrefab = null;
        [SerializeField] GameObject speechTextPrefab = null;
        [SerializeField] protected Transform optionParent = null;
        [SerializeField] GameObject optionPrefab = null;
        [Header("Parameters")]
        [SerializeField] bool handleGlobalInput = true;
        [SerializeField] float delayToDestroyWindow = 0.1f; // Seconds
        [SerializeField] float delayBetweenCharacters = 0.05f; // Seconds
        [Header("Choices")]
        [SerializeField] protected KeyCode choiceInteractKey = KeyCode.E;

        // State -- Message Writing
        bool isWriting = false;
        bool interruptWriting = false;
        bool queuePageClear = false;
        Queue<ReceptacleTextPair> printQueue = new Queue<ReceptacleTextPair>();
        List<GameObject> printedJobs = new List<GameObject>();
        bool destroyQueued = false;
        List<CallbackMessagePair> disableCallbacks = new List<CallbackMessagePair>();

        // State -- Choices
        protected DialogueChoiceOption highlightedChoiceOption = null;

        // Cached References
        PlayerController playerController = null;

        // Structures
        private struct ReceptacleTextPair
        {
            public GameObject receptacle;
            public string text;
        }

        private struct CallbackMessagePair
        {
            public IDialogueBoxCallbackReceiver receiver;
            public string message;
        }

        protected virtual void Awake()
        {
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        }

        protected virtual void OnEnable()
        {
            playerController.globalInput += HandleGlobalInput;
        }

        protected virtual void OnDisable()
        {
            playerController.globalInput -= HandleGlobalInput;
            foreach(CallbackMessagePair callbackMessagePair in disableCallbacks)
            {
                callbackMessagePair.receiver.HandleDialogueCallback(callbackMessagePair.message);
            }
        }

        protected virtual void Update()
        {
            if (!isWriting && printQueue.Count != 0)
            {
                StartCoroutine(TextScan(printQueue.Dequeue()));
            }
            HandleChoiceInput();
        }

        public void AddSimpleText(string text)
        {
            GameObject textObject = Instantiate(simpleTextPrefab, dialogueParent);
            QueueTextForPrinting(textObject, text);
        }

        public void AddSimpleSpeech(string text)
        {
            GameObject textObject = Instantiate(speechTextPrefab, dialogueParent);
            QueueTextForPrinting(textObject, text);
        }

        public void AddPageBreak()
        {
            QueueTextForPrinting(null, "BREAK");
        }

        public void SetDisableCallback(IDialogueBoxCallbackReceiver callbackReceiver, string callbackMessage)
        {
            CallbackMessagePair callbackMessagePair = new CallbackMessagePair
            {
                receiver = callbackReceiver,
                message = callbackMessage
            };
            disableCallbacks.Add(callbackMessagePair);
        }

        private void QueueTextForPrinting(GameObject textObject, string text)
        {
            ReceptacleTextPair receptacleTextPair = new ReceptacleTextPair
            {
                receptacle = textObject,
                text = text
            };
            printQueue.Enqueue(receptacleTextPair);
        }

        private IEnumerator TextScan(ReceptacleTextPair receptacleTextPair)
        {
            if (receptacleTextPair.receptacle == null)
            {
                isWriting = true;
                queuePageClear = true;
                yield break;
            }

            if (string.IsNullOrWhiteSpace(receptacleTextPair.text)) { yield break; }
            receptacleTextPair.receptacle.SetActive(true);

            isWriting = true;
            SimpleTextLink simpleTextLink = receptacleTextPair.receptacle.GetComponent<SimpleTextLink>();

            int letterIndex = 0;
            string textFragment = "";
            while (letterIndex < receptacleTextPair.text.Length - 1)
            {
                if (interruptWriting) { break; }
                textFragment += receptacleTextPair.text[letterIndex];
                simpleTextLink.Setup(textFragment);
                letterIndex++;
                yield return new WaitForSeconds(delayBetweenCharacters);
            }
            simpleTextLink.Setup(receptacleTextPair.text);
            printedJobs.Add(receptacleTextPair.receptacle);
            isWriting = false;
            interruptWriting = false;
        }

        private void ClearPrintedJobs()
        {
            foreach (GameObject printedJob in printedJobs)
            {
                Destroy(printedJob);
            }
        }

        private void HandleChoiceInput()
        {
            // TODO:  Hook up with dialogue system
            // TODO:  Implement new unity input system
            if (!IsChoiceAvailable()) { return; }

            if (ShowCursorOnAnyInteraction()) { return; }
            if (Choose()) { return; }
            if (MoveCursor()) { return; }
        }

        protected virtual bool IsChoiceAvailable()
        {
            return false;
            // TODO:  Hook up with dialogue system
        }

        protected virtual bool ShowCursorOnAnyInteraction()
        {
            if (highlightedChoiceOption == null && (Input.GetKeyDown(choiceInteractKey) ||
                Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S)))
            {
                // TODO:  Hook up with dialogue system
                return true;
            }
            return false;
        }

        protected virtual bool MoveCursor()
        {
            if (highlightedChoiceOption == null) { return false; }

            // TODO:  Hook up with dialogue system
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.S))
            {
                return true;
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A))
            {
                return true;
            }
            return false;
        }

        private bool Choose()
        {
            if (Input.GetKeyDown(choiceInteractKey) && highlightedChoiceOption != null)
            {
                highlightedChoiceOption.GetComponent<Button>().onClick.Invoke();
                return true;
            }
            return false;
        }

        // Global Input Handling
        public void HandleGlobalInput(string interactButtonOne = "Fire1")
        {
            if (!handleGlobalInput) { return; }
            if (Input.GetButtonDown(interactButtonOne))
            {
                if (isWriting)
                {
                    interruptWriting = true;
                    if (queuePageClear)
                    {
                        ClearPrintedJobs();
                        isWriting = false;
                        interruptWriting = false;
                        queuePageClear = false;
                    }
                }
                else
                {
                    if (!destroyQueued)
                    {
                        destroyQueued = true;
                        Destroy(gameObject, delayToDestroyWindow);
                    }
                }
            }
        }
    }
}