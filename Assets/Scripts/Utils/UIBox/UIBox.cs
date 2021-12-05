using Frankie.Control;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Utils.UI
{
    public abstract class UIBox : MonoBehaviour, IGlobalInputReceiver, IUIBoxCallbackReceiver
    {
        // Tunables
        [Header("UI Box Parameters")]
        [SerializeField] protected CanvasGroup canvasGroup = null;
        [SerializeField] protected bool handleGlobalInput = true;
        [SerializeField] bool clearVolatileOptionsOnEnable = true;
        [SerializeField] bool preventEscapeOptionExit = false;
        [Header("Choice Behavior")]
        [SerializeField] protected Transform optionParent = null;
        [SerializeField] protected GameObject optionPrefab = null;

        // State -- Standard
        protected bool destroyQueued = false;
        List<CallbackMessagePair> disableCallbacks = new List<CallbackMessagePair>();
        protected IStandardPlayerInputCaller controller = null;

        // State -- Choices
        bool isChoiceAvailable = false;
        bool clearDisableCallbacksOnChoose = false;
        protected List<UIChoiceOption> choiceOptions = new List<UIChoiceOption>();
        protected UIChoiceOption highlightedChoiceOption = null;

        // Data Structures
        protected struct CallbackMessagePair
        {
            public IUIBoxCallbackReceiver receiver;
            public Action action;
        }

        // Events
        public event Action<UIBoxModifiedType, bool> uiBoxModified;

        #region UnityMethods
        protected virtual void OnEnable()
        {
            StandardOnEnable();
        }

        protected void StandardOnEnable()
        {
            if (controller != null && handleGlobalInput)
            {
                controller.globalInput += HandleGlobalInputWrapper;
            }
            SetUpChoiceOptions();
        }

        protected virtual void OnDisable()
        {
            StandardOnDisable();
        }

        protected void StandardOnDisable()
        {
            ClearChoiceSelections();

            if (controller != null && handleGlobalInput)
            {
                controller.globalInput -= HandleGlobalInputWrapper;
            }

            foreach (CallbackMessagePair callbackMessagePair in disableCallbacks)
            {
                callbackMessagePair.receiver.HandleDisableCallback(this, callbackMessagePair.action);
            }
        }

        private void LateUpdate()
        {
            if (destroyQueued) { Destroy(gameObject); }
        }

        protected virtual void EnableInput(bool enable)
        {
            handleGlobalInput = enable;
        }

        protected void SetVisible(bool enable)
        {
            canvasGroup.alpha = enable ? 1.0f : 0.0f;
        }

        protected void OnUIBoxModified(UIBoxModifiedType dialogueBoxModifiedType, bool enable)
        {
            if (uiBoxModified != null)
            {
                uiBoxModified.Invoke(dialogueBoxModifiedType, enable);
            }
        }

        protected void HandleClientEntry()
        {
            OnUIBoxModified(UIBoxModifiedType.clientEnter, true);
        }

        protected void HandleClientExit()
        {
            OnUIBoxModified(UIBoxModifiedType.clientExit, true);
        }
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
            choiceOptions.AddRange(optionParent.gameObject.GetComponentsInChildren<UIChoiceOption>().OrderBy(x => x.choiceOrder).ToList());

            if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
            else { isChoiceAvailable = false; }
        }

        public void OverrideChoiceOptions(List<ChoiceActionPair> choiceActionPairs)
        {
            choiceOptions.Clear();
            foreach (ChoiceActionPair choiceActionPair in choiceActionPairs)
            {
                AddChoiceOption(choiceActionPair.choice, choiceActionPair.action);
            }
            isChoiceAvailable = true;
        }

        private void AddChoiceOption(string choiceText, Action action)
        {
            UIChoiceOption dialogueChoiceOption = AddChoiceOptionTemplate(choiceText);
            dialogueChoiceOption.GetButton().onClick.AddListener(delegate { StandardChoiceExecution(action); });
        }

        private UIChoiceOption AddChoiceOptionTemplate(string choiceText)
        {
            GameObject uiChoiceOptionObject = Instantiate(optionPrefab, optionParent);
            UIChoiceOption uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceOption>();
            uiChoiceOption.SetChoiceOrder(choiceOptions.Count + 1);
            uiChoiceOption.SetText(choiceText);
            choiceOptions.Add(uiChoiceOption);
            return uiChoiceOption;
        }

        protected virtual void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (UIChoiceOption choiceOption in choiceOptions)
            {
                choiceOption.Highlight(false);
            }
        }

        // Pass through implementations
        protected virtual bool PrepareChooseAction(PlayerInputType playerInputType)
        {
            return StandardPrepareChooseAction(playerInputType);
        }
        protected bool StandardPrepareChooseAction(PlayerInputType playerInputType)
        {
            // Choose(null) since not passing a nodeID, not a standard dialogue -- irrelevant in context of override
            if (playerInputType == PlayerInputType.Execute)
            {
                return Choose(null);
            }
            return false;
        }

        protected virtual bool Choose(string nodeID)
        {
            return StandardChoose(nodeID);
        }

        protected bool StandardChoose(string chooseDetail)
        {
            if (highlightedChoiceOption != null)
            {
                highlightedChoiceOption.GetButton().onClick.Invoke();
                return true;
            }
            return false;
        }

        private void StandardChoiceExecution(Action action)
        {
            if (clearDisableCallbacksOnChoose) { ClearDisableCallbacks(); }
            action?.Invoke();
            Destroy(gameObject);
        }
        #endregion

        #region InputHandling
        protected bool ShowCursorOnAnyInteraction(PlayerInputType playerInputType)
        {
            if (!isChoiceAvailable || choiceOptions.Count == 0) { return false; }
            if (playerInputType == PlayerInputType.DefaultNone || playerInputType == PlayerInputType.Cancel || playerInputType == PlayerInputType.Option) { return false; }

            if (highlightedChoiceOption == null)
            {
                highlightedChoiceOption = choiceOptions[0];
                highlightedChoiceOption.Highlight(true);
                return true;
            }
            return false;
        }

        protected virtual bool MoveCursor(PlayerInputType playerInputType)
        {
            if (!isChoiceAvailable || highlightedChoiceOption == null) { return false; }

            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            bool validInput = MoveCursor(playerInputType, ref choiceIndex, choiceOptions.Count);

            if (validInput)
            {
                ClearChoiceSelections();
                highlightedChoiceOption = choiceOptions[choiceIndex];
                choiceOptions[choiceIndex].Highlight(true);
                return true;
            }
            return false;
        }

        private bool MoveCursor(PlayerInputType playerInputType, ref int currentSelectionIndex, int optionsCount)
        {
            bool validInput = false;
            if (playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
            {
                if (currentSelectionIndex + 1 >= optionsCount) { currentSelectionIndex = 0; }
                else { currentSelectionIndex++; }
                validInput = true;
            }
            else if (playerInputType == PlayerInputType.NavigateUp || playerInputType == PlayerInputType.NavigateLeft)
            {
                if (currentSelectionIndex <= 0) { currentSelectionIndex = optionsCount - 1; }
                else { currentSelectionIndex--; }
                validInput = true;
            }
            return validInput;
        }

        protected bool MoveCursor2D(PlayerInputType playerInputType, ref int choiceIndex)
        {
            return MoveCursor2D(playerInputType, ref choiceIndex, choiceOptions.Count);
        }

        protected bool MoveCursor2D(PlayerInputType playerInputType, ref int choiceIndex, int optionsCount)
        {
            bool validInput = false;
            if (optionsCount == 1)
            {
                choiceIndex = 0;
                validInput = true;
            }
            else if (playerInputType == PlayerInputType.NavigateRight)
            {
                if (choiceIndex + 1 >= optionsCount) { choiceIndex = 0; }
                else { choiceIndex++; }
                validInput = true;
            }
            else if (playerInputType == PlayerInputType.NavigateLeft)
            {
                if (choiceIndex <= 0) { choiceIndex = optionsCount - 1; }
                else { choiceIndex--; }
                validInput = true;
            }
            else if (playerInputType == PlayerInputType.NavigateDown)
            {
                if (choiceIndex + 2 >= optionsCount) { choiceIndex = 0; }
                else { choiceIndex++; choiceIndex++; }
                validInput = true;
            }
            else if (playerInputType == PlayerInputType.NavigateUp)
            {
                if (choiceIndex <= 1) { choiceIndex = optionsCount - 1; }
                else { choiceIndex--; choiceIndex--; }
                validInput = true;
            }
            return validInput;
        }
        #endregion

        #region Input Handling
        public void TakeControl(IStandardPlayerInputCaller standardPlayerInputCaller, IUIBoxCallbackReceiver callbackReceiver, IEnumerable<Action> actions)
        {
            // Only use for passing from non-UI box to UI box
            SetGlobalInputHandler(standardPlayerInputCaller);
            SetDisableCallback(callbackReceiver, actions);
        }

        public void PassControl(UIBox delegateUIBox)
        {
            PassControl(this, new Action[] { () => EnableInput(true) }, delegateUIBox, controller);
        }

        public void PassControl(IUIBoxCallbackReceiver callbackReceiver, IEnumerable<Action> actions, UIBox delegateUIBox, IStandardPlayerInputCaller standardPlayerInputCaller)
        {
            // Disable callback MUST include a re-enable
            EnableInput(false);
            delegateUIBox.SetGlobalInputHandler(standardPlayerInputCaller);
            delegateUIBox.SetDisableCallback(callbackReceiver, actions);
        }

        public void SetGlobalInput(bool enable)
        {
            handleGlobalInput = enable;
        }

        private void SetGlobalInputHandler(IStandardPlayerInputCaller globalInputHandler)
        {
            if (globalInputHandler == null) { return; }

            handleGlobalInput = true;
            controller = globalInputHandler;

            if (gameObject.activeSelf)
            {
                globalInputHandler.globalInput += HandleGlobalInputWrapper; // Unsubscribed on OnDisable
            }
            // No behavior if disabled, will subscribe by OnEnable
        }

        private void HandleGlobalInputWrapper(PlayerInputType playerInputType)
        {
            HandleGlobalInput(playerInputType);
        }

        public virtual bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            return StandardHandleGlobalInput(playerInputType);
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
                    receiver = callbackReceiver,
                    action = action
                };
                disableCallbacks.Add(callbackMessagePair);
            }
        }

        public void ClearDisableCallbacksOnChoose(bool enable)
        {
            clearDisableCallbacksOnChoose = enable;
        }

        public void ClearDisableCallbacks()
        {
            disableCallbacks.Clear();
        }

        public void HandleDisableCallback(IUIBoxCallbackReceiver uiBox, Action action)
        {
            action?.Invoke();
        }
        #endregion

        #region PassThrough
        protected bool StandardHandleGlobalInput(PlayerInputType playerInputType)
        {
            if (HandleGlobalInputSpoofAndExit(playerInputType)) { return true; }

            if (!IsChoiceAvailable()) { return false; } // Childed objects can still accept input on no choices available
            if (ShowCursorOnAnyInteraction(playerInputType)) { return true; }
            if (PrepareChooseAction(playerInputType)) { return true; }
            if (MoveCursor(playerInputType)) { return true; }

            return false;
        }

        protected bool HandleGlobalInputSpoofAndExit(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (preventEscapeOptionExit) { return false; } // Used for main menus that cannot be bypassed -- e.g. start menu
            if (playerInputType == PlayerInputType.Cancel || playerInputType == PlayerInputType.Option)
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
