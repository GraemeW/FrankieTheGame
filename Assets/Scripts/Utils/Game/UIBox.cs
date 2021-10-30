using Frankie.Control;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils
{
    public abstract class UIBox : MonoBehaviour, IGlobalInputReceiver, IUIBoxCallbackReceiver
    {
        // Tunables
        [Header("UI Box Parameters")]
        [SerializeField] protected CanvasGroup canvasGroup = null;
        [SerializeField] protected bool handleGlobalInput = true;

        // State
        protected bool destroyQueued = false;
        List<CallbackMessagePair> disableCallbacks = new List<CallbackMessagePair>();
        protected IStandardPlayerInputCaller controller = null;

        // Data Structures
        protected struct CallbackMessagePair
        {
            public IUIBoxCallbackReceiver receiver;
            public Action action;
        }

        // Events
        public event Action<UIBoxModifiedType, bool> uiBoxModified;

        // Abstract Methods
        protected abstract bool ShowCursorOnAnyInteraction(PlayerInputType playerInputType);
        protected abstract bool IsChoiceAvailable();
        protected abstract bool MoveCursor(PlayerInputType playerInputType);

        // Default Methods
        protected virtual void OnEnable()
        {
            if (controller != null && handleGlobalInput)
            {
                controller.globalInput += HandleGlobalInputWrapper;
            }
        }

        protected virtual void OnDisable()
        {
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

        // Input Handling
        protected bool MoveCursor(PlayerInputType playerInputType, ref int currentSelectionIndex, int optionsCount)
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

        // Callback handling
        public void SetGlobalCallbacks(IStandardPlayerInputCaller globalCallbackSender)
        {
            if (globalCallbackSender == null) { handleGlobalInput = false; return; }

            handleGlobalInput = true;
            controller = globalCallbackSender;

            SubscribeToCallbackSender(globalCallbackSender);
        }

        private void SubscribeToCallbackSender(IStandardPlayerInputCaller globalCallbackSender)
        {
            if (gameObject.activeSelf)
            {
                globalCallbackSender.globalInput += HandleGlobalInputWrapper; // Unsubscribed on OnDisable
            }
            // No behavior if disabled, will subscribe by OnEnable
        }

        public void SetDisableCallback(IUIBoxCallbackReceiver callbackReceiver, Action action)
        {
            CallbackMessagePair callbackMessagePair = new CallbackMessagePair
            {
                receiver = callbackReceiver,
                action = action
            };
            disableCallbacks.Add(callbackMessagePair);
        }

        public void ClearDisableCallbacks()
        {
            disableCallbacks.Clear();
        }


        // Interfaces
        private void HandleGlobalInputWrapper(PlayerInputType playerInputType)
        {
            HandleGlobalInput(playerInputType);
        }

        public virtual bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType == PlayerInputType.Cancel || playerInputType == PlayerInputType.Option)
            {
                HandleClientExit();
                destroyQueued = true;
                return true;
            }

            return false;
        }

        public void HandleDisableCallback(IUIBoxCallbackReceiver uiBox, Action action)
        {
            if (action != null)
            {
                action.Invoke();
            }
        }
    }
}
