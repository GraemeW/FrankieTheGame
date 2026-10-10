using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Speech.UI;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class NamingConfirmPanel : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Animation Parameters")]
        [SerializeField] private float plantedWalkMinAnimationRate = 0.25f;
        [SerializeField] private float plantedWalkMaxAnimationRate = 0.75f;
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmPhrase;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedRejectText;
        [Header("Hookups")]
        [SerializeField] private DialogueBox confirmPhraseBox; // Own panel text scan

        // State
        private readonly NamingConfirmModel namingConfirmModel = new();
        private readonly List<AnimatorSpriteSource> characterSources = new();
        private IUIChoice confirmChoice;
        private IUIChoice rejectChoice;

        // Cached References
        private NameScreenOrchestrator nameScreenOrchestrator;

        // Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedConfirmPhrase.TableEntryReference,
                localizedConfirmText.TableEntryReference,
                localizedRejectText.TableEntryReference,
            };
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            preventEscapeOptionExit = true;
            clearVolatileOptionsOnEnable = false;
            nameScreenOrchestrator = GetComponentInParent<NameScreenOrchestrator>();

            GetComponent<UIToolkitMenuView>().SetDataSource(namingConfirmModel);
            if (confirmPhraseBox != null) { confirmPhraseBox.SetHandleGlobalInput(false); }
            confirmChoice = AddNonDestroyChoiceOption(localizedConfirmText.GetSafeLocalizedString(), Confirm);
            rejectChoice = AddNonDestroyChoiceOption(localizedRejectText.GetSafeLocalizedString(), Reject);
        }

        protected override void StartTriggered()
        {
            if (nameScreenOrchestrator != null && nameScreenOrchestrator.TryGetController(out BaseController baseController)) { baseController.AddInputReceiver(this, null); }
        }

        protected override void EnableTriggered()
        {
            SubscribeToStateUpdates(true);
            confirmChoice.SetText(localizedConfirmText.GetSafeLocalizedString());
            rejectChoice.SetText(localizedRejectText.GetSafeLocalizedString());
        }

        protected override void DisableTriggered()
        {
            SubscribeToStateUpdates(false);
            ClearCards();
        }
        #endregion

        #region EventHandling
        private void SubscribeToStateUpdates(bool enable)
        {
            if (nameScreenOrchestrator == null) { return; }

            nameScreenOrchestrator.stateChanged -= HandleStateChange;
            if (enable) { nameScreenOrchestrator.stateChanged += HandleStateChange; }
        }

        private void HandleStateChange(NameScreenState nameScreenState, NameScreenQuestion _)
        {
            if (nameScreenState != NameScreenState.Confirm) { return; }

            SetupConfirmPhrase();
            SetupCards();
        }

        private void Confirm()
        {
            if (nameScreenOrchestrator != null) { nameScreenOrchestrator.ConfirmAndContinue(); }
        }

        private void Reject()
        {
            if (nameScreenOrchestrator != null) { nameScreenOrchestrator.ResetState(); }
        }
        #endregion

        #region PrivateMethods
        private void SetupConfirmPhrase()
        {
            if (confirmPhraseBox == null) { return; }
            confirmPhraseBox.ClearOldDialogue();
            confirmPhraseBox.Setup(localizedConfirmPhrase.GetSafeLocalizedString());
        }

        private void SetupCards()
        {
            if (nameScreenOrchestrator == null) { return; }

            ClearCards();
            List<NameScreenAnswer> answers = nameScreenOrchestrator.GetAnswers();
            if (answers == null || answers.Count == 0) // Invalid state
            {
                nameScreenOrchestrator.ResetState();
                return;
            }

            var characterCards = new List<ConfirmCharacterCard>();
            var answerCards = new List<ConfirmAnswerCard>();
            foreach (NameScreenAnswer answer in answers.Where(answer => answer.question != null))
            {
                switch (answer.question.questionType)
                {
                    case NameScreenQuestionType.CharacterName:
                        characterCards.Add(new ConfirmCharacterCard(answer.answer, CreateCharacterSprite(answer.question.GetCharacterPrefab())));
                        break;
                    case NameScreenQuestionType.FavouriteFood:
                    case NameScreenQuestionType.FavouriteThing:
                    case NameScreenQuestionType.FrameFlavour:
                        answerCards.Add(new ConfirmAnswerCard(answer.question.localizedQuestion.GetSafeLocalizedString(), answer.answer));
                        break;
                }
            }
            namingConfirmModel.characterCards = characterCards;
            namingConfirmModel.answerCards = answerCards;
        }

        private SpriteModel CreateCharacterSprite(GameObject characterPrefab)
        {
            if (!AnimatorSpriteSource.TryCreate(characterPrefab, transform, out AnimatorSpriteSource characterSource)) { return null; }

            characterSources.Add(characterSource);
            float animationRate = Random.Range(plantedWalkMinAnimationRate, plantedWalkMaxAnimationRate);
            characterSource.PoseWalk(Vector2.down, animationRate);
            return characterSource.spriteModel;
        }

        private void ClearCards()
        {
            namingConfirmModel.characterCards = null;
            namingConfirmModel.answerCards = null;
            foreach (AnimatorSpriteSource characterSource in characterSources.Where(characterSource => characterSource != null)) { Destroy(characterSource.gameObject); }
            characterSources.Clear();
        }
        #endregion
    }
}
