using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Control;

namespace Frankie.Utils.UI
{
    public abstract class UIBox<TBoxState> : UIBoxBase, IInputReceiver where TBoxState : struct, Enum
    {
        // Key State Parameters
        private EnumLookupBase<UIBoxStateBehaviour> stateLookup = new EnumLookup<TBoxState,UIBoxStateBehaviour>();
        
        // State
        private Enum internalUIState = default(TBoxState);
        protected TBoxState uiState
        {
            get => (TBoxState)internalUIState;
            set => internalUIState = value;
        } 
        private Coroutine controllerCheckCoroutine;
        
        // Event Handlers
        public Action<ControllerInputType> GetInputHandler() => HandleInputWrapper;
        protected sealed override void SimpleTriggerUIBoxModified(ReceiverModifiedType dialogueBoxModifiedType) => TriggerUIBoxModified(dialogueBoxModifiedType, new ReceiverModifiedData(this));
        
        // UIBox Configuration
        protected virtual EnumLookup<TBoxState,UIBoxStateBehaviour> BuildStateBehaviours() { return new EnumLookup<TBoxState,UIBoxStateBehaviour>(); } // Note:  Empty/null entries fall back to Standard UIBox Implementation
        protected virtual bool TryAcquireDependencies() => true;
        protected virtual void AwakeTriggered() { }
        protected virtual void StartTriggered() { }
        protected virtual void EnableTriggered() { }
        protected virtual void DisabledTriggered() { }
        protected virtual void DestroyTriggered() { }
        
        #region UnityMethods
        // Note:  UIBox Unity Lifecycle methods are DANGEROUS to override if not considering base implementation
        // Thus: seal base implementation and override from within the methods
        private void Awake()
        {
            if (!TryAcquireDependencies())
            {
                Debug.LogWarning($"Failed to acquire required dependencies.  Destroying UIBox[{name}].");
                Destroy(gameObject);
                return;
            }
            stateLookup = BuildStateBehaviours();
            
            if (!preventEscapeOptionExit && backExitPrefab != null)
            {
                UIBackExit backExit = Instantiate(backExitPrefab, backExitParent != null ? backExitParent : optionParent);
                backExit.SetBackExitClickBehaviour(() => HandleInputWrapper(ControllerInputType.Escape));
            }
            AwakeTriggered();
        }
        
        private void Start()
        {
            TriggerUIBoxModified(ReceiverModifiedType.ClientEnter, new ReceiverModifiedData(this));
            if (controllerCheckCoroutine != null) { StopCoroutine(controllerCheckCoroutine); }
            controllerCheckCoroutine = StartCoroutine(DestroyIfControllerMissing());
            StartTriggered();
        }

        private void OnEnable()
        {
            TriggerUIBoxModified(ReceiverModifiedType.ClientEnable, new ReceiverModifiedData(this));
            SetUpChoiceOptions();
            EnableTriggered();
        }

        private void OnDisable()
        {
            TriggerUIBoxModified(ReceiverModifiedType.ClientDisable, new ReceiverModifiedData(this));
            ClearChoiceSelections();
            if (controllerCheckCoroutine != null) { StopCoroutine(controllerCheckCoroutine); }
            DisabledTriggered();
        }

        private void LateUpdate()
        {
            if (!destroyQueued) { return; }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            TriggerUIBoxModified(ReceiverModifiedType.ClientExit, new ReceiverModifiedData(this));
            DestroyTriggered();
        }
        
        private IEnumerator DestroyIfControllerMissing()
        {
            yield return null;
            if (controller == null && handleGlobalInput)
            {
                Debug.LogWarning($"Failed to find required controller.  Destroying UIBox[{name}].");
                destroyQueued = true;
            }
        }
        #endregion
        
        #region StrategyConfiguratioMethods
        protected void SetUpChoiceOptions()
        {
            if (stateLookup.TryGet(uiState, out UIBoxStateBehaviour stateBehaviour) && stateBehaviour.setupChoiceOptions != null) { stateBehaviour.setupChoiceOptions(); return; }
            
            if (clearVolatileOptionsOnEnable) { choiceOptions.Clear(); }
            List<UIChoice> uiChoices = optionParent.gameObject.GetComponentsInChildren<UIChoice>().OrderBy(x => x.choiceOrder).ToList();
            List<UIChoice> filteredUIChoices = FilterOutSubOptions(uiChoices);
            choiceOptions.AddRange(filteredUIChoices);
            ReconcileChoiceOptions();
        }
        
        protected void ReconcileChoiceOptions()
        {
            if (stateLookup.TryGet(uiState, out UIBoxStateBehaviour stateBehaviour) && stateBehaviour.reconcileChoiceOptions != null) { stateBehaviour.reconcileChoiceOptions(); return; }
            StandardReconcileChoiceOptions();
        }

        protected void StandardReconcileChoiceOptions()
        {
            choiceOptions.RemoveAll(choiceOption => choiceOption == null);
            SetChoiceAvailable(choiceOptions.Count > 0);
        }
        
        protected bool PrepareChooseAction(ControllerInputType controllerInputType)
        {
            if (stateLookup.TryGet(uiState, out UIBoxStateBehaviour stateBehaviour) && stateBehaviour.prepareChooseAction != null) { return stateBehaviour.prepareChooseAction(controllerInputType); }
            return StandardPrepareChooseAction(controllerInputType);
        }
        
        protected bool Choose(string nodeID)
        {
            if (stateLookup.TryGet(uiState, out UIBoxStateBehaviour stateBehaviour) && stateBehaviour.choose != null) { return stateBehaviour.choose(nodeID); }
            return StandardChoose(nodeID);
        }
        
        // NOTE:  When applying alternate implementation, ensure to take care of bool[handleGlobalInput] - to disable input when disabled, return true on !handleGlobalInput
        private bool HandleGlobalInput(ControllerInputType controllerInputType)
        {
            if (stateLookup.TryGet(uiState, out UIBoxStateBehaviour stateBehaviour) && stateBehaviour.handleGlobalInput != null) { return stateBehaviour.handleGlobalInput(controllerInputType); }
            return StandardHandleGlobalInput(controllerInputType);
        }
        
        protected bool MoveCursor(ControllerInputType controllerInputType, CursorMovementStyle cursorMovementStyle)
        {
            if (stateLookup.TryGet(uiState, out UIBoxStateBehaviour stateBehaviour) && stateBehaviour.moveCursor != null) { return stateBehaviour.moveCursor(controllerInputType, cursorMovementStyle); }
            return StandardMoveCursor(controllerInputType, cursorMovementStyle);
        }
        
        private bool IsBackInput(ControllerInputType controllerInputType)
        {
            if (stateLookup.TryGet(uiState, out UIBoxStateBehaviour stateBehaviour) && stateBehaviour.isBackInput != null) { return stateBehaviour.isBackInput(controllerInputType); }
            return controllerInputType is ControllerInputType.Cancel or ControllerInputType.Option;
        }
        private bool TryHandleBackNavigation(ControllerInputType controllerInputType)
        {
            if (stateLookup.TryGet(uiState, out UIBoxStateBehaviour stateBehaviour) && stateBehaviour.tryHandleBackNavigation != null) { return stateBehaviour.tryHandleBackNavigation(controllerInputType); }
            return false;
        }
        #endregion
        
        #region ChoiceExecution
        public void OverrideChoiceOptions(List<ChoiceActionPair> choiceActionPairs)
        {
            choiceOptions.Clear();
            if (choiceActionPairs != null)
            {
                foreach (ChoiceActionPair choiceActionPair in choiceActionPairs)
                {
                    AddChoiceOption(choiceActionPair.choice, choiceActionPair.action);
                }
            }
            ReconcileChoiceOptions();
        }
        
        // Choose(null) if not passing a nodeID / not a dialogue
        protected bool StandardPrepareChooseAction(ControllerInputType controllerInputType) => controllerInputType == ControllerInputType.Execute && Choose(null);
        #endregion
        
        #region InputHandling
        public void SetActiveInput(bool enable)
        {
            ReconcileChoiceOptions();
            handleGlobalInput = enable;
        }
        
        private void HandleInputWrapper(ControllerInputType controllerInputType) => HandleGlobalInput(controllerInputType);
        
        protected bool StandardHandleGlobalInput(ControllerInputType controllerInputType)
        {
            if (!handleGlobalInput) { return true; }

            if (IsBackInput(controllerInputType) && TryHandleBackNavigation(controllerInputType)) { return true; }
            if (TryEarlyExit(controllerInputType)) { return true; }

            if (!IsChoiceAvailable()) { return false; }
            if (ShowCursorOnAnyInteraction(controllerInputType)) { return true; }
            if (PrepareChooseAction(controllerInputType)) { return true; }
            if (MoveCursor(controllerInputType, CursorMovementStyle.Combined)) { return true; }

            return false;
        }
        
        protected bool ShowCursorOnAnyInteraction(ControllerInputType controllerInputType)
        {
            ReconcileChoiceOptions();
            if (!IsChoiceAvailable()) { return false; }
            if (controllerInputType is ControllerInputType.DefaultNone or ControllerInputType.Cancel or ControllerInputType.Option) { return false; }

            if (highlightedChoiceOption == null && choiceOptions.Count > 0)
            {
                highlightedChoiceOption = choiceOptions[0];
                highlightedChoiceOption.Highlight(true);
                return true;
            }
            return false;
        }
        #endregion
    }
}
