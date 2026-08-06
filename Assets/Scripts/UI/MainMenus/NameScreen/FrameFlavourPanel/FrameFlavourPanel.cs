using Frankie.Control;
using Frankie.Saving;
using Frankie.Speech.UI;
using Frankie.Utils;
using Frankie.Utils.UI;
using UnityEngine;

namespace Frankie.Menu.UI.FrameFlavourPanel
{
    public class FrameFlavourPanel : UIBox<UIBoxState>
    {
        [Header("Properties")]
        [SerializeField] private NameScreenQuestion flavourQuestion;
        [Header("Hookups")]
        [SerializeField] private DialogueOptionBox flavourSelectionBox;
        [SerializeField] private Transform flavourChoiceParent;

        // Cached References
        private NameScreenOrchestrator nameScreenOrchestrator;
        
        #region UIBoxConfiguration
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var stateBehaviours = new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            stateBehaviours.TrySet(UIBoxState.Default, new UIBoxStateBehaviour(setupChoiceOptions: ImplementSetupChoiceOptions));
            return stateBehaviours;
        }

        private void ImplementSetupChoiceOptions()
        {
            choiceOptions.Clear();
            foreach (FrameFlavourChoice flavourChoice in flavourChoiceParent.GetComponentsInChildren<FrameFlavourChoice>())
            {
                choiceOptions.Add(flavourChoice);
            }
        }
        #endregion
        
        
        #region UnityMethods
        protected override void AwakeTriggered()
        {
            preventEscapeOptionExit = true;
            nameScreenOrchestrator = GetComponentInParent<NameScreenOrchestrator>();
            flavourSelectionBox.SetHandleGlobalInput(false);
        }
        
        protected override void StartTriggered()
        {
            if (nameScreenOrchestrator != null && nameScreenOrchestrator.TryGetController(out BaseController baseController)) { baseController.AddInputReceiver(this, null); }
        }
        
        protected override void EnableTriggered()
        {
            SetupButtonEvents(true);
        }
        
        protected override void DisableTriggered()
        {
            SetupButtonEvents(false);
        }
        #endregion
        
        #region PrivateMethods
        private void SetupButtonEvents(bool enable)
        {
            foreach (FrameFlavourChoice flavourChoice in flavourChoiceParent.GetComponentsInChildren<FrameFlavourChoice>())
            {
                flavourChoice.RemoveOnClickListeners();
                if (enable) { flavourChoice.AddOnClickListener(() => HandleFlavourSelection(flavourChoice)); }
            }
        }

        private void HandleFlavourSelection(FrameFlavourChoice flavourChoice)
        {
            if (nameScreenOrchestrator == null) { return; }
            if (flavourChoice == null) { nameScreenOrchestrator.AdvanceState(); return; }

            PlayerPrefsController.SetFrameFlavourColour(flavourChoice.GetFrameFlavourColour());
            nameScreenOrchestrator.AddAnswer(new NameScreenAnswer(flavourQuestion, flavourChoice.GetFrameFlavour(), flavourChoice.GetFrameFlavourColour()));
            nameScreenOrchestrator.AdvanceState();
        }
        #endregion
    }
}
