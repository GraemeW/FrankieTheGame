using System;
using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Control;
using LowDefMustard.Utils;

namespace LowDefMustard.UIBox.Tests
{
    // BuildStateBehaviours() defaults to production's own empty EnumLookup
    //  - dispatch method falls through to Standard* unless a test calls SetStateBehaviour first
    
    public class TestUIBox : UIBox<UIBoxState>
    {
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

        // Pre-lifecycle hookup wiring
        public void SetHookups(CanvasGroup canvasGroupRef = null, Transform optionParentRef = null,
            Transform backExitParentRef = null, UIBackExit backExitPrefabRef = null,
            GameObject optionButtonPrefabRef = null, GameObject optionSliderPrefabRef = null)
        {
            canvasGroup = canvasGroupRef;
            optionParent = optionParentRef;
            backExitParent = backExitParentRef;
            backExitPrefab = backExitPrefabRef;
            optionButtonPrefab = optionButtonPrefabRef;
            optionSliderPrefab = optionSliderPrefabRef;
        }
        
        // Attributes
        public BaseController publicController => controller;
        public bool publicHandleGlobalInput { get => handleGlobalInput; set => handleGlobalInput = value; }
        public bool publicClearVolatileOptionsOnEnable { get => clearVolatileOptionsOnEnable; set => clearVolatileOptionsOnEnable = value; }
        public bool publicPreventEscapeOptionExit { get => preventEscapeOptionExit; set => preventEscapeOptionExit = value; }
        public UIChoice publicHighlightedChoiceOption => highlightedChoiceOption;
        public IReadOnlyList<UIChoice> publicChoiceOptions => choiceOptions;

        // Getters
        public bool PublicIsChoiceAvailable() => IsChoiceAvailable();
        
        // Setters
        public void SetControllerDirectly(BaseController value) => controller = value;
        public void SetHighlightedChoiceOption(UIChoice choice) => highlightedChoiceOption = choice;
        public void InjectChoiceOptions(IEnumerable<UIChoice> choices)
        {
            choiceOptions.Clear();
            choiceOptions.AddRange(choices);
        }
        public void PublicSetChoiceAvailable(bool enable) => SetChoiceAvailable(enable);
        public void PublicAddChoiceOption(string text, Action action) => AddChoiceOption(text, action);
        public void PublicClearChoiceSelections() => ClearChoiceSelections();
        
        // Public Passthroughs
        public static List<UIChoice> PublicFilterOutSubOptions(List<UIChoice> choices) => FilterOutSubOptions(choices);
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
