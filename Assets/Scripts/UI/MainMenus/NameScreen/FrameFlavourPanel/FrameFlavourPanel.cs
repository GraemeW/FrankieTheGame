using System.Collections.Generic;
using LowDefMustard.Control;
using LowDefMustard.Utils;
using Frankie.Saving;
using Frankie.Speech.UI;
using Frankie.Utils.UI;
using UnityEngine;

namespace Frankie.Menu.UI
{
    public class FrameFlavourPanel : UIBox<UIBoxState>
    {
        [Header("Properties")]
        [SerializeField] private NameScreenQuestion flavourQuestion;
        [Header("Hookups")]
        [SerializeField] private DialogueOptionBox flavourSelectionBox;
        [SerializeField] private Transform flavourChoiceParent;

        // State
        private readonly HashSet<UIFrame> additionalLocalFrameOverwrites = new();
        
        // Cached References
        private NameScreenOrchestrator nameScreenOrchestrator;
        
        #region UIBoxConfiguration
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var stateBehaviours = new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            stateBehaviours.TrySet(UIBoxState.Default, new UIBoxStateBehaviour(
                setupChoiceOptions: ImplementSetupChoiceOptions,
                tryHandleBackNavigation: ImplementTryHandleBackNavigation
                ));
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
        
        private bool ImplementTryHandleBackNavigation(ControllerInputType inputType)
        {
            // Only available if EnableEscapeOptionExit is triggered
            Destroy(gameObject);
            return true;
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
        
        #region PublicMethods
        public void SetupAdditionalColorUpdates(UIBoxBase uiBox)
        {
            // Temporarily update frame colour on selection for already instantiated frames
            foreach (UIFrame uiFrame in uiBox.GetComponentsInChildren<UIFrame>())
            {
                additionalLocalFrameOverwrites.Add(uiFrame);
            }
        }
        
        public void EnableEscapeOptionExit()
        {
            preventEscapeOptionExit = true;
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
            bool hasNewFrameColour = HasNewFrameColour(flavourChoice, out Color newFrameColor);
            if (hasNewFrameColour) { UpdateFrameColour(newFrameColor); }
            
            if (nameScreenOrchestrator == null) { Destroy(gameObject); return; }
            UpdateNameOrchestratorState(flavourChoice, newFrameColor);
        }

        private static bool HasNewFrameColour(FrameFlavourChoice flavourChoice, out Color newFrameColor)
        {
            newFrameColor = Color.white;
            if (flavourChoice == null) { return false; }
            newFrameColor = flavourChoice.GetFrameFlavourColour();
            return true;
        }

        private void UpdateFrameColour(Color newFrameColor)
        {
            PlayerPrefsController.SetFrameFlavourColour(newFrameColor);
            foreach (UIFrame uiFrame in additionalLocalFrameOverwrites) { uiFrame.OverwriteLocalFrameFlavour(newFrameColor); }
        }

        private void UpdateNameOrchestratorState(FrameFlavourChoice flavourChoice, Color newFrameColor)
        {
            if (flavourChoice == null) { nameScreenOrchestrator.AdvanceState(); return; }
            nameScreenOrchestrator.AddAnswer(new NameScreenAnswer(flavourQuestion, flavourChoice.GetFrameFlavour(), newFrameColor));
            nameScreenOrchestrator.AdvanceState();
        }
        #endregion
    }
}
