using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Control;

namespace Frankie.Utils.UI
{
    public abstract class UIBoxBase : MonoBehaviour
    {
        // Tunables
        [Header("UI Box Parameters")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [Header("Choice Behavior")]
        [SerializeField] protected Transform optionParent;
        [SerializeField] protected GameObject optionButtonPrefab;
        [SerializeField] protected GameObject optionSliderPrefab;
        
        // Key State Parameters
        protected bool handleGlobalInput { get; set; } = true;
        protected bool clearVolatileOptionsOnEnable { get; set; } = true;
        protected bool preventEscapeOptionExit { get; set; } = false;
        
        // State -- Standard
        protected BaseController controller;
        public bool destroyQueued { get; set; } = false;

        // State -- Choices
        private bool isChoiceAvailable = false;
        private bool clearDisableCallbacksOnChoose = false;
        protected readonly List<UIChoice> choiceOptions = new();
        protected UIChoice highlightedChoiceOption;

        // Event Handles
        private event Action<ReceiverModifiedType, ReceiverModifiedData> receiverModified;
        
        #region UtilityMethods
        public void SubscribeToReceiverUpdates(bool enable, Action<ReceiverModifiedType, ReceiverModifiedData> action)
        {
            receiverModified -= action;
            if (enable) { receiverModified += action; }
        }
        
        protected void TriggerUIBoxModified(ReceiverModifiedType dialogueBoxModifiedType, ReceiverModifiedData uiBoxModifiedData) => receiverModified?.Invoke(dialogueBoxModifiedType, uiBoxModifiedData);
        protected abstract void SimpleTriggerUIBoxModified(ReceiverModifiedType dialogueBoxModifiedType); // Implemented via UIBox
        protected void SetVisible(bool enable) => canvasGroup.alpha = enable ? 1.0f : 0.0f;
        public void ClearDisableCallbacksOnChoose(bool enable) => clearDisableCallbacksOnChoose = enable;
        public void ClearDisableCallbacks() => SimpleTriggerUIBoxModified(ReceiverModifiedType.ClearDisableCallbacks);
        #endregion

        #region ChoiceSetup
        // Use state variable instead of counting for co-ex with dialogue system
        protected bool IsChoiceAvailable() => isChoiceAvailable;
        protected void SetChoiceAvailable(bool enable) => isChoiceAvailable = enable;

        protected void AddChoiceOption(string choiceText, Action action)
        {
            UIChoiceButton dialogueChoiceOption = AddChoiceOptionTemplate(choiceText);
            dialogueChoiceOption.AddOnClickListener(delegate { StandardChoiceExecution(action); });
        }

        private UIChoiceButton AddChoiceOptionTemplate(string choiceText)
        {
            GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
            var uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
            uiChoiceOption.SetChoiceOrder(choiceOptions.Count + 1);
            uiChoiceOption.SetText(choiceText);
            choiceOptions.Add(uiChoiceOption);
            return uiChoiceOption;
        }

        protected void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (UIChoice choiceOption in choiceOptions.Where(choiceOption => choiceOption != null))
            {
                choiceOption.Highlight(false);
            }
        }
        
        protected static List<UIChoice> FilterOutSubOptions(List<UIChoice> uiChoices)
        {
            List<UIChoice> filteredUIChoices = uiChoices.ToList();
            var subOptions = new List<UIChoice>();
            foreach (UIChoice choice in filteredUIChoices)
            {
                if (choice is UIChoiceContainer uiChoiceContainer)
                {
                    subOptions.AddRange(uiChoiceContainer.GetSubOptions());
                }
            }
            foreach (UIChoice choice in subOptions) { filteredUIChoices.Remove(choice); }
            return filteredUIChoices;
        }
        #endregion
        
        #region ChoiceExecution
        protected bool StandardChoose(string chooseDetail)
        {
            // Note:  chooseDetail ignored in standard implementation -- employed in DialogueBox override
            if (highlightedChoiceOption == null) { return false; }
            highlightedChoiceOption.UseChoice();
            return true;
        }
       
        private void StandardChoiceExecution(Action action)
        {
            if (clearDisableCallbacksOnChoose) { SimpleTriggerUIBoxModified(ReceiverModifiedType.ClearDisableCallbacks); }
            action?.Invoke();
            Destroy(gameObject);
        }
        #endregion

        #region InputHandling
        public bool TrySetController(BaseController setController)
        {
            if (setController == null) { return false; }

            handleGlobalInput = true;
            controller = setController;
            return true;
        }
        
        protected bool StandardMoveCursor(ControllerInputType controllerInputType, CursorMovementStyle cursorMovementStyle)
        {
            if (!isChoiceAvailable || highlightedChoiceOption == null) { return false; }

            // Special objects that require specialty input (sliders, etc.)
            if (highlightedChoiceOption is IUIMoveInterceptor uiMoveInterceptor && uiMoveInterceptor.TryMove(controllerInputType)) { return true; }
            
            // Standard choice handling
            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            bool validInput = TryExecuteMove(controllerInputType, ref choiceIndex, choiceOptions.Count, cursorMovementStyle);
            if (validInput)
            {
                ClearChoiceSelections();
                highlightedChoiceOption = choiceOptions[choiceIndex];
                choiceOptions[choiceIndex].Highlight(true);
                return true;
            }
            return false;
        }

        protected bool MoveCursor2D(ControllerInputType controllerInputType)
        {
            // Standard implementation
            if (!isChoiceAvailable || highlightedChoiceOption == null) { return false; }

            // Special objects that require specialty input (sliders, etc.)
            if (highlightedChoiceOption is IUIMoveInterceptor uiMoveInterceptor && uiMoveInterceptor.TryMove(controllerInputType)) { return true; }
            
            // Standard choice handling
            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            bool validInput = TryExecuteMove2D(controllerInputType, ref choiceIndex, choiceOptions.Count);
            if (validInput)
            {
                ClearChoiceSelections();
                highlightedChoiceOption = choiceOptions[choiceIndex];
                choiceOptions[choiceIndex].Highlight(true);
                return true;
            }
            return false;
        }
        
        protected bool TryEarlyExit(ControllerInputType controllerInputType)
        {
            if (preventEscapeOptionExit) { return false; }
            if (controllerInputType is not (ControllerInputType.Cancel or ControllerInputType.Option or ControllerInputType.Escape)) { return false; }
            destroyQueued = true;
            return true;
        }

        private static bool TryExecuteMove(ControllerInputType controllerInputType, ref int currentSelectionIndex, int optionsCount, CursorMovementStyle cursorMovementStyle)
        {
            bool validInput = false;
            switch (controllerInputType)
            {
                case ControllerInputType.NavigateRight when cursorMovementStyle is CursorMovementStyle.Combined or CursorMovementStyle.Horizontal:
                case ControllerInputType.NavigateDown when cursorMovementStyle is CursorMovementStyle.Combined or CursorMovementStyle.Vertical:
                {
                    if (currentSelectionIndex + 1 >= optionsCount) { currentSelectionIndex = 0; }
                    else { currentSelectionIndex++; }
                    validInput = true;
                    break;
                }
                case ControllerInputType.NavigateUp when cursorMovementStyle is CursorMovementStyle.Combined or CursorMovementStyle.Vertical:
                case ControllerInputType.NavigateLeft when cursorMovementStyle is CursorMovementStyle.Combined or CursorMovementStyle.Horizontal:
                {
                    if (currentSelectionIndex <= 0) { currentSelectionIndex = optionsCount - 1; }
                    else { currentSelectionIndex--; }
                    validInput = true;
                    break;
                }
            }
            return validInput;
        }
        
        private static bool TryExecuteMove2D(ControllerInputType controllerInputType, ref int choiceIndex, int optionsCount)
        {
            bool validInput = false;
            if (optionsCount == 1)
            {
                choiceIndex = 0;
                validInput = true;
            }
            else switch (controllerInputType)
            {
                case ControllerInputType.NavigateRight:
                {
                    if (choiceIndex + 1 >= optionsCount) { choiceIndex = 0; }
                    else { choiceIndex++; }
                    validInput = true;
                    break;
                }
                case ControllerInputType.NavigateLeft:
                {
                    if (choiceIndex <= 0) { choiceIndex = optionsCount - 1; }
                    else { choiceIndex--; }
                    validInput = true;
                    break;
                }
                case ControllerInputType.NavigateDown:
                {
                    if (choiceIndex + 2 >= optionsCount) { choiceIndex = 0; }
                    else { choiceIndex++; choiceIndex++; }
                    validInput = true;
                    break;
                }
                case ControllerInputType.NavigateUp:
                {
                    if (choiceIndex <= 1) { choiceIndex = optionsCount - 1; }
                    else { choiceIndex--; choiceIndex--; }
                    validInput = true;
                    break;
                }
            }
            return validInput;
        }
        #endregion
    }
}
