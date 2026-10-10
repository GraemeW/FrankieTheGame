using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Saving;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class FrameFlavourPanel : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Properties")]
        [SerializeField] private NameScreenQuestion flavourQuestion;
        [SerializeField] private FrameFlavours frameFlavours;

        // State
        private readonly FrameFlavourPanelModel frameFlavourPanelModel = new();
        private readonly List<(FrameFlavourOption flavour, FrameFlavourChoiceModel choiceModel)> flavourChoices = new();

        // Cached References
        private NameScreenOrchestrator nameScreenOrchestrator;

        // Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return frameFlavours != null ? frameFlavours.GetLocalizationEntries() : new List<TableEntryReference>();
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            preventEscapeOptionExit = true;
            clearVolatileOptionsOnEnable = false;
            nameScreenOrchestrator = GetComponentInParent<NameScreenOrchestrator>();

            var menuView = GetComponent<UIToolkitMenuView>();
            menuView.SetDataSource(frameFlavourPanelModel);
            BuildChoiceOptions(menuView);
        }

        protected override void StartTriggered()
        {
            if (nameScreenOrchestrator != null && nameScreenOrchestrator.TryGetController(out BaseController baseController)) { baseController.AddInputReceiver(this, null); }
        }

        protected override void EnableTriggered()
        {
            ResetAllTextElements();
        }

        protected override void DisableTriggered()
        {
            frameFlavourPanelModel.hasPreview = false;
        }
        #endregion

        #region PublicMethods
        public void EnableEscapeOptionExit()
        {
            preventEscapeOptionExit = false;
            SetupBackExitButton();
        }
        #endregion

        #region PrivateMethods
        private void BuildChoiceOptions(UIToolkitMenuView menuView)
        {
            if (frameFlavours == null) { return; }
            foreach (FrameFlavourOption flavour in frameFlavours.GetFlavours())
            {
                if (flavour == null) { continue; }

                var choiceModel = new FrameFlavourChoiceModel { text = flavour.GetName(), colour = flavour.GetColour() };
                var flavourHandle = new FrameFlavourHandle(menuView, choiceModel, () => StandardChoiceExecution(() => ChooseFlavour(flavour), false), () => PreviewFlavour(flavour));
                menuView.AddEntry(flavourHandle);
                choiceOptions.Add(flavourHandle);
                flavourChoices.Add((flavour, choiceModel));
            }
        }

        private void ResetAllTextElements()
        {
            frameFlavourPanelModel.questionText = flavourQuestion != null ? flavourQuestion.localizedQuestion.GetSafeLocalizedString() : "";
            foreach ((FrameFlavourOption flavour, FrameFlavourChoiceModel choiceModel) in flavourChoices) { choiceModel.text = flavour.GetName(); }
        }

        private void PreviewFlavour(FrameFlavourOption flavour)
        {
            frameFlavourPanelModel.previewColour = flavour.GetColour();
            frameFlavourPanelModel.hasPreview = true;
        }

        private void ChooseFlavour(FrameFlavourOption flavour)
        {
            // Note:  Every frame on screen follows the saved flavour
            PlayerPrefsController.SetFrameFlavourColour(flavour.GetColour());

            if (nameScreenOrchestrator == null) { Destroy(gameObject); return; } // Escape Menu path
            nameScreenOrchestrator.AddAnswer(new NameScreenAnswer(flavourQuestion, flavour.GetName(), flavour.GetColour()));
            nameScreenOrchestrator.AdvanceState();
        }
        #endregion
    }
}
