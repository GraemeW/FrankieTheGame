using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Control;

namespace Frankie.Utils.UI
{
    public abstract class UIBox : MonoBehaviour, IGlobalInputReceiver, IUIBoxCallbackReceiver
    {
        // Tunables
        [Header("UI Box Parameters")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected bool handleGlobalInput = true;
        [SerializeField] private bool clearVolatileOptionsOnEnable = true;
        [SerializeField] private bool preventEscapeOptionExit = false;
        [Header("Choice Behavior")]
        [SerializeField] protected Transform optionParent;
        [SerializeField] protected GameObject optionButtonPrefab;
        [SerializeField] protected GameObject optionSliderPrefab;

        // State -- Standard
        protected bool destroyQueued = false;
        private readonly List<CallbackMessagePair> disableCallbacks = new();
        protected BaseController controller;

        // State -- Choices
        private bool isChoiceAvailable = false;
        private bool clearDisableCallbacksOnChoose = false;
        protected readonly List<UIChoice> choiceOptions = new();
        protected UIChoice highlightedChoiceOption;

        // Data Structures
        private struct CallbackMessagePair
        {
            public IUIBoxCallbackReceiver callbackReceiver;
            public Action action;
        }

        // Events
        public event Action<UIBoxModifiedType, bool> uiBoxModified;

        #region StaticMethods
        private static bool MoveCursor(ControllerInputType controllerInputType, ref int currentSelectionIndex, int optionsCount, CursorMovementStyle cursorMovementStyle)
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
        
        private static bool MoveCursor2D(ControllerInputType controllerInputType, ref int choiceIndex, int optionsCount)
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
        
        #region UnityMethods
        protected virtual void OnEnable()
        {
            if (controller != null && handleGlobalInput) { controller.SubscribeToGlobalInput(true, HandleGlobalInputWrapper); }
            SetUpChoiceOptions();
        }

        protected virtual void OnDisable()
        {
            if (controller != null && handleGlobalInput) { controller.SubscribeToGlobalInput(false, HandleGlobalInputWrapper);  }
            ClearChoiceSelections();

            foreach (CallbackMessagePair callbackMessagePair in disableCallbacks)
            {
                callbackMessagePair.callbackReceiver.HandleDisableCallback(this, callbackMessagePair.action);
            }
        }

        private void LateUpdate()
        {
            if (!destroyQueued) { return; }
            Destroy(gameObject);
        }
        #endregion

        #region UtilityMethods
        protected virtual void EnableInput(bool enable) => handleGlobalInput = enable;
        protected void SetVisible(bool enable) => canvasGroup.alpha = enable ? 1.0f : 0.0f; 
        
        protected void TriggerUIBoxModified(UIBoxModifiedType dialogueBoxModifiedType, bool enable) => uiBoxModified?.Invoke(dialogueBoxModifiedType, enable);
        protected void HandleClientEntry() => TriggerUIBoxModified(UIBoxModifiedType.ClientEnter, true);
        protected void HandleClientExit() => TriggerUIBoxModified(UIBoxModifiedType.ClientExit, true);
        #endregion

        #region ChoiceBehavior
        protected bool IsChoiceAvailable()
        {
            // Use state variable instead of counting for co-ex with dialogue system
            return isChoiceAvailable; 
        }

        protected void SetChoiceAvailable(bool enable)
        {
            isChoiceAvailable = enable;
        }

        protected virtual void SetUpChoiceOptions()
        {
            if (clearVolatileOptionsOnEnable) { choiceOptions.Clear(); }

            List<UIChoice> uiChoices = optionParent.gameObject.GetComponentsInChildren<UIChoice>().OrderBy(x => x.choiceOrder).ToList();
            List<UIChoice> filteredUIChoices = FilterOutSubOptions(uiChoices);
            choiceOptions.AddRange(filteredUIChoices);

            isChoiceAvailable = choiceOptions.Count > 0;
        }

        public void OverrideChoiceOptions(List<ChoiceActionPair> choiceActionPairs)
        {
            choiceOptions.Clear();
            if (choiceActionPairs == null) { isChoiceAvailable = false; return; }

            foreach (ChoiceActionPair choiceActionPair in choiceActionPairs)
            {
                AddChoiceOption(choiceActionPair.choice, choiceActionPair.action);
            }
            isChoiceAvailable = true;
        }

        private void AddChoiceOption(string choiceText, Action action)
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
        
        private List<UIChoice> FilterOutSubOptions(List<UIChoice> uiChoices)
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

        protected virtual void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (UIChoice choiceOption in choiceOptions)
            {
                choiceOption.Highlight(false);
            }
        }

        // Pass through implementations
        protected virtual bool PrepareChooseAction(ControllerInputType controllerInputType) => StandardPrepareChooseAction(controllerInputType);
        protected bool StandardPrepareChooseAction(ControllerInputType controllerInputType)
        {
            // Choose(null) since not passing a nodeID, not a standard dialogue -- irrelevant in context of override
            return controllerInputType == ControllerInputType.Execute && Choose(null);
        }

        protected virtual bool Choose(string nodeID) => StandardChoose(nodeID);
        protected bool StandardChoose(string chooseDetail)
        {
            // Note:  chooseDetail ignored in standard implementation -- employed in DialogueBox override
            if (highlightedChoiceOption == null) { return false; }

            highlightedChoiceOption.UseChoice();
            return true;
        }

        private void StandardChoiceExecution(Action action)
        {
            if (clearDisableCallbacksOnChoose) { ClearDisableCallbacks(); }
            action?.Invoke();
            Destroy(gameObject);
        }
        #endregion

        #region InputHandling
        protected bool ShowCursorOnAnyInteraction(ControllerInputType controllerInputType)
        {
            if (!isChoiceAvailable || choiceOptions.Count == 0) { return false; }
            if (controllerInputType is ControllerInputType.DefaultNone or ControllerInputType.Cancel or ControllerInputType.Option) { return false; }

            if (highlightedChoiceOption == null)
            {
                highlightedChoiceOption = choiceOptions[0];
                highlightedChoiceOption.Highlight(true);
                return true;
            }
            return false;
        }

        protected virtual bool MoveCursor(ControllerInputType controllerInputType, CursorMovementStyle cursorMovementStyle)
        {
            if (!isChoiceAvailable || highlightedChoiceOption == null) { return false; }

            // Special objects that require specialty input (sliders, etc.)
            if (highlightedChoiceOption is IUIMoveInterceptor uiMoveInterceptor && uiMoveInterceptor.TryMove(controllerInputType)) { return true; }
            
            // Standard choice handling
            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            bool validInput = MoveCursor(controllerInputType, ref choiceIndex, choiceOptions.Count, cursorMovementStyle);
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
            bool validInput = MoveCursor2D(controllerInputType, ref choiceIndex, choiceOptions.Count);
            if (validInput)
            {
                ClearChoiceSelections();
                highlightedChoiceOption = choiceOptions[choiceIndex];
                choiceOptions[choiceIndex].Highlight(true);
                return true;
            }
            return false;
        }
        #endregion

        #region Input Handling
        public void TakeControl(BaseController baseController, IUIBoxCallbackReceiver callbackReceiver, IEnumerable<Action> onDisableActions)
        {
            // Only use for passing from non-UI box to UI box
            SetGlobalInputHandler(baseController);
            SetDisableCallback(callbackReceiver, onDisableActions);
        }
        
        public void SetGlobalInput(bool enable) => handleGlobalInput = enable;
        
        public virtual bool HandleGlobalInput(ControllerInputType controllerInputType)
        {
            // NOTE:  When overriding, ensure to handle the bool:  handleGlobalInput
            // To disable input when disabled, return true on !handleGlobalInput, or otherwise use HandleGlobalInputSpoofAndExit()
            return StandardHandleGlobalInput(controllerInputType);
        }
        
        // Match HandleGlobalInput with no return
        private void HandleGlobalInputWrapper(ControllerInputType controllerInputType) => HandleGlobalInput(controllerInputType);

        protected void PassControl(UIBox delegateUIBox)
        {
            PassControl(this, new Action[] { () => EnableInput(true) }, delegateUIBox, controller);
        }
        
        protected void PassControlToClose(UIBox delegateUIBox)
        {
            PassControl(this, new Action[] { () => EnableInput(true), () => destroyQueued = true }, delegateUIBox, controller);
        }

        protected void PassControl(IUIBoxCallbackReceiver callbackReceiver, IEnumerable<Action> actions, UIBox delegateUIBox, BaseController baseController)
        {
            // Disable callback MUST include a re-enable
            EnableInput(false);
            delegateUIBox.SetGlobalInputHandler(baseController);
            delegateUIBox.SetDisableCallback(callbackReceiver, actions);
        }

        private void SetGlobalInputHandler(BaseController globalInputHandler)
        {
            if (globalInputHandler == null) { return; }

            handleGlobalInput = true;
            controller = globalInputHandler;

            if (gameObject.activeSelf) { controller.SubscribeToGlobalInput(true, HandleGlobalInputWrapper); }
            // No behaviour if disabled, will subscribe by OnEnable
        }
        #endregion

        #region CallbackHandling
        private void SetDisableCallback(IUIBoxCallbackReceiver callbackReceiver, IEnumerable<Action> actions)
        {
            if (actions == null) { return; }
            foreach (Action action in actions)
            {
                CallbackMessagePair callbackMessagePair = new CallbackMessagePair
                {
                    callbackReceiver = callbackReceiver,
                    action = action
                };
                disableCallbacks.Add(callbackMessagePair);
            }
        }

        public void ClearDisableCallbacks() => disableCallbacks.Clear();
        public void ClearDisableCallbacksOnChoose(bool enable) => clearDisableCallbacksOnChoose = enable;
        public void HandleDisableCallback(IUIBoxCallbackReceiver callbackReceiver, Action action) => action?.Invoke();
        #endregion

        #region PassThrough
        protected bool StandardHandleGlobalInput(ControllerInputType controllerInputType)
        {
            if (HandleGlobalInputSpoofAndExit(controllerInputType)) { return true; }

            if (!IsChoiceAvailable()) { return false; } // Childed objects can still accept input on no choices available
            if (ShowCursorOnAnyInteraction(controllerInputType)) { return true; }
            if (PrepareChooseAction(controllerInputType)) { return true; }
            if (MoveCursor(controllerInputType, CursorMovementStyle.Combined)) { return true; }

            return false;
        }

        protected bool HandleGlobalInputSpoofAndExit(ControllerInputType controllerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (preventEscapeOptionExit) { return false; } // Used for main menus that cannot be bypassed -- e.g. start menu
            if (controllerInputType is ControllerInputType.Cancel or ControllerInputType.Option or ControllerInputType.Escape)
            {
                HandleClientExit();
                destroyQueued = true;
                return true;
            }
            return false;
        }
        #endregion
    }
}
