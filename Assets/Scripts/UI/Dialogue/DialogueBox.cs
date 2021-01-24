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
        [SerializeField] Transform dialogueParent = null;
        [SerializeField] GameObject simpleTextPrefab = null;
        [SerializeField] GameObject speechTextPrefab = null;
        [SerializeField] Transform optionParent = null;
        [SerializeField] GameObject optionPrefab = null;
        [Header("Parameters")]
        [SerializeField] bool handleGlobalInput = true;
        [SerializeField] float delayBetweenCharacters = 0.05f; // Seconds
        [SerializeField] float delayToDestroyWindow = 0.1f; // Seconds

        // State
        bool isWriting = false;
        bool interruptWriting = false;
        bool queuePageClear = false;
        Queue<ReceptacleTextPair> printQueue = new Queue<ReceptacleTextPair>();
        List<GameObject> printedJobs = new List<GameObject>();
        bool destroyQueued = false;
        List<CallbackMessagePair> disableCallbacks = new List<CallbackMessagePair>();

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

        private void Awake()
        {
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            playerController.globalInput += HandleInput;
        }

        private void OnDisable()
        {
            playerController.globalInput -= HandleInput;
            foreach(CallbackMessagePair callbackMessagePair in disableCallbacks)
            {
                callbackMessagePair.receiver.HandleDialogueCallback(callbackMessagePair.message);
            }
        }

        private void Update()
        {
            if (!isWriting && printQueue.Count != 0)
            {
                StartCoroutine(TextScan(printQueue.Dequeue()));
            }
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

        private void Choose()
        {
            // TODO:  Implement choose action (dependent on dialogue system
            // Takes DialogueNode as input instead of string;;
        }

        private void ClearPrintedJobs()
        {
            foreach (GameObject printedJob in printedJobs)
            {
                Destroy(printedJob);
            }
        }

        private void ClearDialogue()
        {
            foreach (Transform child in dialogueParent)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in optionParent)
            {
                Destroy(child.gameObject);
            }
        }

        // Global Input Handling
        public void HandleInput(string interactButtonOne = "Fire1")
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