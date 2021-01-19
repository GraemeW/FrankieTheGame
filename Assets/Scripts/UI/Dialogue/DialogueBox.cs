using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Frankie.Dialogue.UI
{
    public class DialogueBox : MonoBehaviour
    {
        // Tunables
        [Header("Links And Prefabs")]
        [SerializeField] Transform dialogueParent = null;
        [SerializeField] GameObject simpleTextPrefab = null;
        [SerializeField] GameObject speechTextPrefab = null;
        [SerializeField] Transform optionParent = null;
        [SerializeField] GameObject optionPrefab = null;
        [Header("Presentation Parameters")]
        [SerializeField] float delayBetweenCharacters = 0.05f; // Seconds

        // State
        bool isWriting = false;
        Queue<ReceptacleTextPair> printQueue = new Queue<ReceptacleTextPair>();
        
        // Structures
        private struct ReceptacleTextPair
        {
            public GameObject receptacle;
            public string text;
        }

        private void Update()
        {
            if (!isWriting && printQueue.Count != 0)
            {
                StartCoroutine(TextScan(printQueue.Dequeue()));
            }
        }

        public void BreakTextScan()
        {
            isWriting = false;
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
            if (string.IsNullOrWhiteSpace(receptacleTextPair.text)) { yield break; }
            receptacleTextPair.receptacle.SetActive(true);

            isWriting = true;
            SimpleTextLink simpleTextLink = receptacleTextPair.receptacle.GetComponent<SimpleTextLink>();

            int letterIndex = 0;
            string textFragment = "";
            while (letterIndex < receptacleTextPair.text.Length - 1)
            {
                if (!isWriting) { break; }
                textFragment += receptacleTextPair.text[letterIndex];
                simpleTextLink.Setup(textFragment);
                letterIndex++;
                yield return new WaitForSeconds(delayBetweenCharacters);
            }
            simpleTextLink.Setup(receptacleTextPair.text);
            isWriting = false;
        }

        private void Choose()
        {
            // TODO:  Implement choose action (dependent on dialogue system
            // Takes DialogueNode as input instead of string;;
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
    }
}