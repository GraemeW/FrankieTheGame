using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using LowDefMustard.Control;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Speech.UI;
using Frankie.Utils.UI;
using UnityEngine.Localization.Tables;

namespace Frankie.Menu.UI
{
    public class NamingConfirmPanel : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Properties")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmPhrase;
        [Header("Prefabs")]
        [SerializeField] private CharacterConfirmCard characterConfirmCardPrefab;
        [SerializeField] private AltConfirmCard altConfirmCardPrefab;
        [Header("Hookups")]
        [SerializeField] private DialogueBox confirmPhraseBox; // Using prefab for box & text scan only
        [SerializeField] private DialogueOptionBox confirmOptionBox; // Using prefab for box & access to standard buttons (below) 
        [SerializeField] private UIChoiceButton confirmButton;
        [SerializeField] private UIChoiceButton rejectButton;
        [SerializeField] private Transform leftCardSpawnPoint;
        [SerializeField] private Transform rightCardSpawnPoint;

        // Cached References
        private NameScreenOrchestrator nameScreenOrchestrator;

        // Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedConfirmPhrase.TableEntryReference,
            };
        }
        
        // UIBox Configuration
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var stateBehaviours = new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            stateBehaviours.TrySet(UIBoxState.Default, new UIBoxStateBehaviour(setupChoiceOptions: ImplementSetupChoiceOptions));
            return stateBehaviours;
        }
        
        #region UnityMethods
        protected override void AwakeTriggered()
        {
            preventEscapeOptionExit = true;
            nameScreenOrchestrator = GetComponentInParent<NameScreenOrchestrator>();
            confirmPhraseBox.SetHandleGlobalInput(false);
            confirmOptionBox.SetHandleGlobalInput(false);
        }

        protected override void StartTriggered()
        {
            if (nameScreenOrchestrator != null && nameScreenOrchestrator.TryGetController(out BaseController baseController)) { baseController.AddInputReceiver(this, null); }
        }

        protected override void EnableTriggered()
        {
            SubscribeToStateUpdates(true);
            SetupButtonEvents(true);
        }
        
        protected override void DisableTriggered()
        {
            SubscribeToStateUpdates(false);
            SetupButtonEvents(false);
        }
        #endregion
        
        #region UIBoxConfiguration
        private void ImplementSetupChoiceOptions()
        {
            choiceOptions.Clear();
            choiceOptions.Add(confirmButton);
            choiceOptions.Add(rejectButton);
        }
        #endregion
        
        #region EventHandling
        private void SubscribeToStateUpdates(bool enable)
        {
            if (nameScreenOrchestrator == null)  { return; }

            nameScreenOrchestrator.stateChanged -= HandleStateChange;
            if (enable) { nameScreenOrchestrator.stateChanged += HandleStateChange; }
        }

        private void HandleStateChange(NameScreenState nameScreenState, NameScreenQuestion _)
        {
            if (nameScreenState != NameScreenState.Confirm) { return; }

            SetupConfirmPhrase();
            SetupConfirmationCards();
        }

        private void SetupButtonEvents(bool enable)
        {
            confirmButton.RemoveOnClickListeners();
            rejectButton.RemoveOnClickListeners();
            if (enable && nameScreenOrchestrator != null)
            {
                confirmButton.AddOnClickListener(() => nameScreenOrchestrator.ConfirmAndContinue());
                rejectButton.AddOnClickListener(() => nameScreenOrchestrator.ResetState());
            }
        }
        #endregion
        
        #region PrivateMethods
        private void SetupConfirmPhrase()
        {
            confirmPhraseBox.ClearOldDialogue();
            confirmPhraseBox.Setup(localizedConfirmPhrase.GetSafeLocalizedString());
        }
        
        private void SetupConfirmationCards()
        {
            if (nameScreenOrchestrator == null) { return; }
            
            foreach (Transform child in leftCardSpawnPoint) { Destroy(child.gameObject); }
            foreach (Transform child in rightCardSpawnPoint) { Destroy(child.gameObject); }
            
            List<NameScreenAnswer> answers = nameScreenOrchestrator.GetAnswers();
            if (answers == null || answers.Count == 0) // Invalid state
            {
                nameScreenOrchestrator.ResetState();
                return;
            }

            foreach (NameScreenAnswer answer in answers.Where(answer => answer.question != null))
            {
                switch (answer.question.questionType)
                {
                    case NameScreenQuestionType.CharacterName:
                        CharacterConfirmCard characterConfirmCard = Instantiate(characterConfirmCardPrefab, leftCardSpawnPoint);
                        characterConfirmCard.Setup(answer.answer, answer.question.thingPrefab);
                        break;
                    case NameScreenQuestionType.FavouriteFood:
                    case NameScreenQuestionType.FavouriteThing:
                    case NameScreenQuestionType.FrameFlavour:
                        AltConfirmCard altConfirmCard = Instantiate(altConfirmCardPrefab, rightCardSpawnPoint);
                        altConfirmCard.Setup(answer.question.localizedQuestion.GetSafeLocalizedString(), answer.answer);
                        break;
                }
            }
        }
        
        #endregion
    }
}
