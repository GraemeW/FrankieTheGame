using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    public class NameScreenOrchestrator : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] private MainMenuController mainMenuController;
        [Header("Tunables")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString startingMessage;
        [SerializeField] private List<NameScreenQuestion> questions = new();
        [SerializeField] private float thingSize = 180;
        [SerializeField] private float thingWalkTimeEstimate = 2f;
        [Header("Prefabs")]
        [SerializeField] private DialogueBox dialogueBoxPrefab;
        [SerializeField] private UIChoiceButton choiceButtonPrefab;
        [Header("Hookups")]
        [SerializeField] private Transform infoPanel;
        [SerializeField] private Transform namingPanel;
        [SerializeField] private Transform confirmationPanel;
        [SerializeField] private InputDisplay inputDisplay;
        [SerializeField] private DialogueBox questionTextScan;
        [SerializeField] private Keyboard keyboard;
        [SerializeField] private Transform offStagePosition;
        [SerializeField] private Transform stagePosition;

        // State
        private NameScreenState nameScreenState = NameScreenState.Intro;
        private int questionIndex = 0;
        private GameObject thing;
        private Coroutine thingInitializationCoroutine;
        private Coroutine thingRemovalCoroutine;
        
        #region UnityMethods
        private void Awake()
        {
            questionTextScan.SetHandleGlobalInput(false);
            mainMenuController.AddInputReceiver(keyboard, null);

            SetState(NameScreenState.Intro);
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoPanel);
            dialogueBox.Setup(startingMessage.GetSafeLocalizedString());
            mainMenuController.AddInputReceiver(dialogueBox, () => SetState(NameScreenState.Naming));
        }
        
        private void Start()
        {
            keyboard.Setup(this, inputDisplay);
        }

        private void OnDestroy()
        {
            if (thingInitializationCoroutine != null) { StopCoroutine(thingInitializationCoroutine); }
        }
        #endregion
        
        #region PrivateMethods

        private void SetState(NameScreenState setNameScreenState)
        {
            nameScreenState = setNameScreenState;
            
            infoPanel.gameObject.SetActive(setNameScreenState == NameScreenState.Intro);
            namingPanel.gameObject.SetActive(setNameScreenState == NameScreenState.Naming);
            confirmationPanel.gameObject.SetActive(setNameScreenState == NameScreenState.Confirm);
            
            if (setNameScreenState == NameScreenState.Naming) { InitiateNamingRoutine(); }
        }

        private void InitiateNamingRoutine()
        {
            if (questions.Count == 0) { SetState(NameScreenState.Confirm); }
            questionIndex = 0;
            SetupCurrentQuestion();
        }

        public void AdvanceNamingRoutine()
        {
            // TODO:  Store current data ++ error handling for name
            
            questionIndex++;
            SetupCurrentQuestion();
        }

        private void SetupCurrentQuestion()
        {
            if (questionIndex >= questions.Count) { SetState(NameScreenState.Confirm); return; }
            
            inputDisplay.ClearDisplay();
            
            NameScreenQuestion question = questions[questionIndex];
            questionTextScan.ClearOldDialogue();
            questionTextScan.Setup(question.question);
            
            InitializeThing(question.thingPrefab);
            
            keyboard.SetDontCareAnswers(question.dontCareAnswers);
        }

        private void InitializeThing(GameObject newThingPrefab)
        {
            GameObject newThing = null;
            if (newThingPrefab != null) { newThing = Instantiate(newThingPrefab, offStagePosition); }
            if (thingInitializationCoroutine != null) { StopCoroutine(thingInitializationCoroutine); }
            thingInitializationCoroutine = StartCoroutine(SwapThingToWalkInFrame(newThing));
        }
        
        private IEnumerator SwapThingToWalkInFrame(GameObject newThing)
        {
            UICharacter uiCharacter;
            
            if (thing != null)
            {
                if (thing.TryGetComponent(out uiCharacter))
                {
                    uiCharacter.MoveTowards(offStagePosition.position);
                    yield return new WaitForSeconds(thingWalkTimeEstimate);
                }
                Destroy(thing);
            }
            thing = newThing;
            if (thing == null) { yield break; }
            
            yield return null;
            if (thing.TryGetComponent(out RectTransform rectTransform)) { rectTransform.sizeDelta = new Vector2(thingSize, thingSize); }
            yield return null;
            if (thing.TryGetComponent(out uiCharacter)) { uiCharacter.MoveTowards(stagePosition.position); }
            else { thing.transform.position = stagePosition.position; }
        }
        #endregion
    }
}
