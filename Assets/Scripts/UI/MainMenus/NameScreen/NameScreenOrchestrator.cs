using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

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
        [SerializeField] private Transform confirmationPanel;
        [SerializeField] private Transform dialogueBoxSpawnPoint;

        // State
        private NameScreenState nameScreenState = NameScreenState.Intro;
        private int questionIndex = 0;
        
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
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, dialogueBoxSpawnPoint);
            if (dialogueBox.TryGetComponent(out RectTransform rectTransform)) { rectTransform.anchoredPosition = Vector2.zero; } // Revert any prefab offsets
            dialogueBox.Setup(startingMessage.GetSafeLocalizedString());
            mainMenuController.AddInputReceiver(dialogueBox, () => SetState(NameScreenState.Naming));
        }
        #endregion
        
        #region PublicMethods
        public bool TryGetController(out BaseController controller)
        {
            controller = mainMenuController;
            return controller != null;
        }
        
        public void SetState(NameScreenState setNameScreenState)
        {
            nameScreenState = setNameScreenState;
            
            infoPanel.gameObject.SetActive(setNameScreenState is NameScreenState.Intro);
            namingPanel.gameObject.SetActive(setNameScreenState is NameScreenState.Naming or NameScreenState.NamingComplete);
            confirmationPanel.gameObject.SetActive(setNameScreenState is NameScreenState.Confirm);
            
            if (setNameScreenState == NameScreenState.Intro) { questionIndex = 0; }
            
            NameScreenQuestion question = HasValidQuestion() ? questions[questionIndex] : null;
            if (setNameScreenState == NameScreenState.Naming && question == null) { SetState(NameScreenState.Confirm); } // Invalid state
            
            stateChanged?.Invoke(nameScreenState, question);
        }
        #endregion
        
        #region PrivateMethods
        private bool HasValidQuestion() => questions is { Count: > 0 } && questionIndex < questions.Count;

        public void AdvanceNamingRoutine()
        {
            questionIndex++;
            SetState(HasValidQuestion() ? NameScreenState.Naming : NameScreenState.NamingComplete);
        }
        #endregion
    }
}
