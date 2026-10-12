using System;
using System.Collections.Generic;
using LowDefMustard.Control;
using LowDefMustard.Utils;

namespace LowDefMustard.UIBox.Tests
{
    public class TestUIBox : UIBox<UIBoxState>
    {
        // Note:  BuildStateBehaviours() defaults to production's own empty EnumLookup
        //  - dispatch method falls through to Standard* unless a test calls SetStateBehaviour first
        
        // UI Testing Properties
        public int awakeTriggeredCount;
        public int startTriggeredCount;
        public int enableTriggeredCount;
        public int disableTriggeredCount;
        public int destroyTriggeredCount;
        public bool tryAcquireDependenciesResult = true;

        // Unity LifeCycle
        protected override bool TryAcquireDependencies() => tryAcquireDependenciesResult;
        protected override void AwakeTriggered() => awakeTriggeredCount++;
        protected override void StartTriggered() => startTriggeredCount++;
        protected override void EnableTriggered() => enableTriggeredCount++;
        protected override void DisableTriggered() => disableTriggeredCount++;
        protected override void DestroyTriggered() => destroyTriggeredCount++;

        // State Behaviours
        // Must be called before activation - BuildStateBehaviours() only runs once from Awake
        private EnumLookup<UIBoxState, UIBoxStateBehaviour> stateBehavioursOverride;
        public void SetStateBehaviour(UIBoxState state, UIBoxStateBehaviour behaviour)
        {
            stateBehavioursOverride ??= new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            stateBehavioursOverride.TrySet(state, behaviour);
        }
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours() =>
            stateBehavioursOverride ?? base.BuildStateBehaviours();

        // Attributes
        public BaseController publicController => controller;
        public bool publicHandleGlobalInput { get => handleGlobalInput; set => handleGlobalInput = value; }
        public bool publicClearChoiceOptionsOnEnable { get => clearChoiceOptionsOnEnable; set => clearChoiceOptionsOnEnable = value; }
        public bool publicPreventEscapeOptionExit { get => preventEscapeOptionExit; set => preventEscapeOptionExit = value; }
        public IUIChoice publicHighlightedChoiceOption => highlightedChoiceOption;
        public IReadOnlyList<IUIChoice> publicChoiceOptions => choiceOptions;

        // Getters
        public bool PublicIsChoiceAvailable() => IsChoiceAvailable();
        
        // Setters
        public void SetControllerDirectly(BaseController value) => controller = value;
        public void SetHighlightedChoiceOption(IUIChoice choice) => highlightedChoiceOption = choice;
        public void InjectChoiceOptions(IEnumerable<IUIChoice> choices)
        {
            choiceOptions.Clear();
            choiceOptions.AddRange(choices);
        }
        public void PublicSetChoiceAvailable(bool enable) => SetChoiceAvailable(enable);
        public IUIChoice PublicAddChoiceOption(string text, Action action) => AddChoiceOption(text, action);
        public IUIChoice PublicAddNonDestroyChoiceOption(string text, Action action, Action onHighlight = null) => AddNonDestroyChoiceOption(text, action, onHighlight);
        public void PublicClearChoiceSelections() => ClearChoiceSelections();
        
        // Public Passthroughs
        public bool PublicStandardChoose(string detail) => StandardChoose(detail);
        public bool PublicStandardMoveCursor(ControllerInputType input, CursorMovementStyle style) => StandardMoveCursor(input, style);
        public bool PublicMoveCursor2D(ControllerInputType input) => MoveCursor2D(input);
        public bool PublicStandardMoveCursorSpatial(ControllerInputType input) => StandardMoveCursorSpatial(input);
        public bool PublicTryEarlyExit(ControllerInputType input) => TryEarlyExit(input);
        public void PublicSetUpChoiceOptions() => SetUpChoiceOptions();
        public void PublicReconcileChoiceOptions() => ReconcileChoiceOptions();
        public bool PublicPrepareChooseAction(ControllerInputType input) => PrepareChooseAction(input);
        public bool PublicChoose(string nodeID) => Choose(nodeID);
        public bool PublicMoveCursor(ControllerInputType input, CursorMovementStyle style) => MoveCursor(input, style);
        public bool PublicStandardHandleGlobalInput(ControllerInputType input) => StandardHandleGlobalInput(input);
        public bool PublicShowCursorOnAnyInteraction(ControllerInputType input) => ShowCursorOnAnyInteraction(input);
        public void PublicSetupBackExitButton() => SetupBackExitButton();
    }
}
