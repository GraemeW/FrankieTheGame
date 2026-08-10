using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Control;
using Frankie.Saving;
using Frankie.ZoneManagement;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class NameScreenOrchestrator : MonoBehaviour, ILocalizable
    {
        [Header("Controller")]
        [SerializeField] private MainMenuController mainMenuController;
        [Header("Tunables")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString startingMessage;
        [SerializeField] private List<NameScreenQuestion> questions = new();
        [Header("Prefabs")]
        [SerializeField] private DialogueBox dialogueBoxPrefab;
        [SerializeField] private UIChoiceButton choiceButtonPrefab;
        [Header("Hookups")]
        [SerializeField] private Transform infoPanel;
        [SerializeField] private Transform namingPanel;
        [SerializeField] private Transform frameFlavourPanel;
        [SerializeField] private Transform namingConfirmPanel;
        [SerializeField] private Transform dialogueBoxSpawnPoint;

        // State
        private NameScreenState nameScreenState = NameScreenState.Intro;
        private int questionIndex = 0;
        private readonly List<NameScreenAnswer> answers = new();
        
        // Events
        public event Action<NameScreenState, NameScreenQuestion> stateChanged;
        
        // Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                startingMessage.TableEntryReference,
            };
        }
        
        #region UnityMethods
        private void Awake()
        {
            SetState(NameScreenState.Intro);
        }
        #endregion
        
        #region PublicMethods
        public bool TryGetController(out BaseController controller)
        {
            controller = mainMenuController;
            return controller != null;
        }
        
        public List<NameScreenAnswer> GetAnswers() => answers;
        public void AddAnswer(NameScreenAnswer answer) => answers.Add(answer);

        public void AdvanceState() => SetState(nameScreenState.NextClamped());

        public void ResetState()
        {
            questionIndex = 0;
            answers.Clear();
            PlayerPrefsController.SetFrameFlavourColour(Color.white);
            SetState(NameScreenState.Intro);
        }
        
        public void AdvanceNamingRoutine(string answerText)
        {
            if (!HasValidQuestion()) { SetState(NameScreenState.Confirm); return; } // Invalid state
            
            NameScreenQuestion currentQuestion = questions[questionIndex];
            answers.Add(new NameScreenAnswer(currentQuestion, answerText));
            
            questionIndex++;
            SetState(HasValidQuestion() ? NameScreenState.Naming : NameScreenState.NamingComplete);
        }

        public void ConfirmAndContinue()
        {
            foreach (NameScreenAnswer answer in answers.Where(answer => answer.question != null))
            {
                switch (answer.question.questionType)
                {
                    case NameScreenQuestionType.CharacterName:
                        CharacterProperties characterProperties = answer.question.optionalCharacterProperties;
                        if (characterProperties == null) { continue; }
                        PlayerPrefsController.SetCharacterName(characterProperties.GetCharacterID(), answer.answer);
                        break;
                    case NameScreenQuestionType.FavouriteFood:
                        PlayerPrefsController.SetFavouriteFood(answer.answer);
                        break;
                    case NameScreenQuestionType.FavouriteThing:
                        PlayerPrefsController.SetFavouriteThing(answer.answer);
                        break;
                    case NameScreenQuestionType.FrameFlavour:
                        PlayerPrefsController.SetFrameFlavourColour(answer.optionalAnswerColor);
                        break;
                    default:
                        continue;
                }
            }
            var sceneQueueData = new SceneQueueData(() => SavingWrapper.Save(), 0f, false);
            SceneLoader.QueueScene(SceneQueueType.New, sceneQueueData);
        }
        #endregion
        
        #region PrivateMethods
        private bool HasValidQuestion() => questions is { Count: > 0 } && questionIndex < questions.Count;

        private void SetState(NameScreenState setNameScreenState)
        {
            nameScreenState = setNameScreenState;
            
            infoPanel.gameObject.SetActive(setNameScreenState is NameScreenState.Intro);
            namingPanel.gameObject.SetActive(setNameScreenState is NameScreenState.Naming or NameScreenState.NamingComplete);
            frameFlavourPanel.gameObject.SetActive(setNameScreenState is NameScreenState.FrameFlavouring);
            namingConfirmPanel.gameObject.SetActive(setNameScreenState is NameScreenState.Confirm);

            if (setNameScreenState == NameScreenState.Intro) { SpawnIntroDialogueBox(); }
            
            NameScreenQuestion question = HasValidQuestion() ? questions[questionIndex] : null;
            if (setNameScreenState == NameScreenState.Naming && question == null) { SetState(NameScreenState.Confirm); return; } // Invalid state
            
            stateChanged?.Invoke(nameScreenState, question);
        }
        
        private void SpawnIntroDialogueBox()
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, dialogueBoxSpawnPoint);
            if (dialogueBox.TryGetComponent(out RectTransform rectTransform)) { rectTransform.anchoredPosition = Vector2.zero; } // Revert any prefab offsets
            dialogueBox.Setup(startingMessage.GetSafeLocalizedString());
            mainMenuController.AddInputReceiver(dialogueBox, () => SetState(NameScreenState.Naming));
        }
        #endregion
    }
}
