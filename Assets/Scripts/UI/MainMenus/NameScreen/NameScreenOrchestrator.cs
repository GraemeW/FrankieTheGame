using System;
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
        [SerializeField] private Transform dialogueBoxSpawnPoint;
        [SerializeField] private Transform offStagePosition;
        [SerializeField] private Transform stagePosition;
        [SerializeField] private RelativeUISequencer offStagePositionRelativeUI;
        [SerializeField] private RelativeUISequencer leftWalkCoverRelativeUI;

        // State
        private NameScreenState nameScreenState = NameScreenState.Intro;
        private int questionIndex = 0;
        private GameObject thing;
        private Coroutine thingInitializationCoroutine;
        private Coroutine thingRemovalCoroutine;
        
        // Events
        public event Action<NameScreenState, NameScreenQuestion> stateChanged;
        
        #region UnityMethods
        private void Awake()
        {
            SetState(NameScreenState.Intro);
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, dialogueBoxSpawnPoint);
            if (dialogueBox.TryGetComponent(out RectTransform rectTransform)) { rectTransform.anchoredPosition = Vector2.zero; } // Revert any prefab offsets
            dialogueBox.Setup(startingMessage.GetSafeLocalizedString());
            mainMenuController.AddInputReceiver(dialogueBox, () => SetState(NameScreenState.Naming));
        }

        private void OnDestroy()
        {
            if (thingInitializationCoroutine != null) { StopCoroutine(thingInitializationCoroutine); }
        }
        #endregion
        
        #region PublicMethods
        public bool TryGetController(out BaseController controller)
        {
            controller = mainMenuController;
            return controller != null;
        }
        #endregion
        
        #region PrivateMethods
        private void SetState(NameScreenState setNameScreenState)
        {
            nameScreenState = setNameScreenState;
            
            infoPanel.gameObject.SetActive(setNameScreenState == NameScreenState.Intro);
            namingPanel.gameObject.SetActive(setNameScreenState == NameScreenState.Naming);
            confirmationPanel.gameObject.SetActive(setNameScreenState == NameScreenState.Confirm);
            
            if (setNameScreenState == NameScreenState.Intro) { questionIndex = 0; }
            
            NameScreenQuestion question = HasValidQuestion() ? questions[questionIndex] : null;
            if (setNameScreenState == NameScreenState.Naming && question == null) { SetState(NameScreenState.Confirm); } // Invalid state
            
            stateChanged?.Invoke(setNameScreenState, question);
        }

        private bool HasValidQuestion() => questions is { Count: > 0 } && questionIndex < questions.Count;

        public void AdvanceNamingRoutine()
        {
            questionIndex++;
            SetState(HasValidQuestion() ? NameScreenState.Naming : NameScreenState.Confirm);
        }

        public void InitializeThing(GameObject newThingPrefab)
        {
            if (thingInitializationCoroutine != null) { StopCoroutine(thingInitializationCoroutine); }
            thingInitializationCoroutine = StartCoroutine(SwapThingToWalkInFrame(newThingPrefab));
        }
        
        private IEnumerator SwapThingToWalkInFrame(GameObject newThingPrefab)
        {
            yield return null;
            if (offStagePositionRelativeUI != null) { offStagePositionRelativeUI.AssertAlignment(); }
            if (leftWalkCoverRelativeUI != null) { leftWalkCoverRelativeUI.AssertAlignment(); }
            yield return null;
            
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
            if (newThingPrefab == null) { thing = null; yield break;}
            
            GameObject newThing = Instantiate(newThingPrefab, offStagePosition);
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
