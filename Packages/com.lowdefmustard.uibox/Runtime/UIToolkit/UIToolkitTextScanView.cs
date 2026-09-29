using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // UXML requirements:  a TextEntryContainer; optionally a ChoiceEntryContainer and BackExitButton
    // View state (entries, visibility, layout, back-exit) is held here as models and (re)applied whenever the PanelRenderer (re)loads its UI
    [RequireComponent(typeof(PanelRenderer))]
    public class UIToolkitTextScanView : MonoBehaviour, IUIBoxView, ITextScanView
    {
        // State
        private bool isVisible = true;
        private Action backExitAction;
        private ChoiceLayout choiceLayout = ChoiceLayout.Horizontal;
        private readonly List<EntryHandle> entries = new();
        private int boundUIVersion = -1;

        // Cached References
        private PanelRenderer panelRenderer;
        private VisualElement boundRoot;
        private TextEntryContainer textEntryContainer;
        private ChoiceEntryContainer choiceEntryContainer;
        private BackExitButton backExitButton;

        #region UnityMethods
        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            // Note:  Invoked immediately if the UI is already loaded, then again on every reload (e.g. UXML live reload)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        }

        private void OnDisable()
        {
            // Note:  PanelRenderer preserves UI content while disabled - no unbind required
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        }
        #endregion

        #region IUIBoxView
        public void SetVisible(bool enable)
        {
            isVisible = enable;
            ApplyVisibility();
        }

        public void SetBackExitAction(Action onBackExit)
        {
            backExitAction = onBackExit;
            ApplyBackExit();
        }
        #endregion

        #region ITextScanView
        public ITextScanEntry CreateTextEntry(TextEntryType textEntryType)
        {
            var entryHandle = new EntryHandle(this, new TextEntryModel(), textEntryType, null);
            AddEntry(entryHandle);
            return entryHandle;
        }

        public ITextScanChoiceEntry CreateChoiceEntry(string text, int choiceOrder, Action onChoose)
        {
            var choiceEntryModel = new ChoiceEntryModel { text = text };
            var entryHandle = new EntryHandle(this, choiceEntryModel, TextEntryType.Simple, onChoose);
            AddEntry(entryHandle);
            return entryHandle;
        }

        public void SetChoiceLayout(ChoiceLayout setChoiceLayout)
        {
            choiceLayout = setChoiceLayout;
            ApplyChoiceLayout();
        }

        public void ClearEntries()
        {
            foreach (EntryHandle entryHandle in entries)
            {
                entryHandle.DetachElement();
                entryHandle.isRemoved = true;
            }
            entries.Clear();
        }
        #endregion

        #region PrivateMethods
        private void HandleUIReload(PanelRenderer reloadedPanelRenderer, VisualElement rootElement, int version)
        {
            if (version == boundUIVersion) { return; } // Re-enable without a UI change -- existing binding still valid
            boundUIVersion = version;
            BindRoot(rootElement);
        }

        private void BindRoot(VisualElement rootElement)
        {
            UnbindRoot();

            textEntryContainer = rootElement.Q<TextEntryContainer>();
            if (textEntryContainer == null)
            {
                Debug.LogWarning($"UIToolkitTextScanView[{name}] failed to bind:  UXML requires a TextEntryContainer.");
                return;
            }

            boundRoot = rootElement;
            boundRoot.pickingMode = PickingMode.Ignore; // Only interactive children should intercept pointer input
            choiceEntryContainer = boundRoot.Q<ChoiceEntryContainer>();
            backExitButton = boundRoot.Q<BackExitButton>();
            if (backExitButton != null) { backExitButton.clicked += HandleBackExitClicked; }

            foreach (EntryHandle entryHandle in entries) { AttachElement(entryHandle); }
            ApplyVisibility();
            ApplyBackExit();
            ApplyChoiceLayout();
        }

        private void UnbindRoot()
        {
            if (backExitButton != null) { backExitButton.clicked -= HandleBackExitClicked; }
            foreach (EntryHandle entryHandle in entries) { entryHandle.DetachElement(); }

            boundRoot = null;
            textEntryContainer = null;
            choiceEntryContainer = null;
            backExitButton = null;
        }

        private void AddEntry(EntryHandle entryHandle)
        {
            entries.Add(entryHandle);
            AttachElement(entryHandle);
        }

        private void RemoveEntry(EntryHandle entryHandle)
        {
            entryHandle.DetachElement();
            entries.Remove(entryHandle);
        }

        private void AttachElement(EntryHandle entryHandle)
        {
            // Note:  No-op until bound - entries created before the UI loads are attached in BindRoot
            VisualElement container = entryHandle.isChoice ? choiceEntryContainer : textEntryContainer;
            container?.Add(entryHandle.BuildElement());
        }

        private void ApplyVisibility()
        {
            if (boundRoot == null) { return; }
            boundRoot.style.visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }

        private void ApplyBackExit()
        {
            backExitButton?.SetShown(backExitAction != null);
        }

        private void ApplyChoiceLayout()
        {
            choiceEntryContainer?.SetLayout(choiceLayout);
        }

        private void HandleBackExitClicked() => backExitAction?.Invoke();
        #endregion

        #region EntryHandle
        private sealed class EntryHandle : ITextScanChoiceEntry
        {
            // State
            private readonly UIToolkitTextScanView view;
            private readonly TextEntryModel model;
            private readonly TextEntryType textEntryType;
            private readonly Action onChoose;
            private VisualElement element;
            public bool isRemoved { get; set; } = false;
            public bool isChoice => model is ChoiceEntryModel;

            public EntryHandle(UIToolkitTextScanView view, TextEntryModel model, TextEntryType textEntryType, Action onChoose)
            {
                this.view = view;
                this.model = model;
                this.textEntryType = textEntryType;
                this.onChoose = onChoose;
            }

            #region ITextScanChoiceEntry
            public bool isAlive => !isRemoved && view != null;
            public bool canDisplayText => true;
            public void Reveal() => model.isRevealed = true;
            public void SetText(string text) => model.text = text;
            public void Highlight(bool enable)
            {
                if (model is ChoiceEntryModel choiceEntryModel) { choiceEntryModel.isHighlighted = enable; }
            }

            public void Remove()
            {
                if (isRemoved) { return; }
                isRemoved = true;
                if (view != null) { view.RemoveEntry(this); }
            }
            #endregion

            #region ElementHandling
            public VisualElement BuildElement()
            {
                DetachElement();
                if (isChoice)
                {
                    var choiceEntryElement = new ChoiceEntryElement();
                    choiceEntryElement.clicked += HandleClicked;
                    element = choiceEntryElement;
                }
                else
                {
                    element = new TextEntryElement(textEntryType);
                }
                element.dataSource = model;
                return element;
            }

            public void DetachElement()
            {
                if (element == null) { return; }
                if (element is ChoiceEntryElement choiceEntryElement) { choiceEntryElement.clicked -= HandleClicked; }
                element.RemoveFromHierarchy();
                element = null;
            }

            private void HandleClicked()
            {
                if (isRemoved) { return; }
                onChoose?.Invoke();
            }
            #endregion
        }
        #endregion
    }
}
