using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // UXML options:  a ChoiceEntryContainer (for CreateChoiceOption) and a BackExitButton
    // View state (entries, visibility, data source, back-exit) is held here and (re)applied whenever the PanelRenderer (re)loads its UI
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class UIToolkitBoxView : MonoBehaviour, IUIBoxView
    {
        // Tunables
        [Tooltip("Applied to the root in order (later sheets win ties) - shared theme styles come from the PanelSettings")]
        [SerializeField] private List<StyleSheet> styleSheets = new();

        // State
        private bool isVisible = true;
        private bool isPointerInputEnabled = true;
        private object dataSource;
        private Action backExitAction;
        private readonly List<EntryHandle> entries = new();
        private readonly Dictionary<Type, VisualElement> containers = new(); // Resolved per bind, by entry container type
        private int boundUIVersion = -1;

        // Cached References
        private PanelRenderer panelRenderer;
        private VisualElement boundRoot;
        private BackExitButton backExitButton;
        protected ChoiceEntryContainer choiceEntryContainer { get; private set; }

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

        #region AbstractMethods
        protected abstract bool TryBindContent(VisualElement rootElement); // Resolve view-specific elements; false aborts the bind
        protected virtual void UnbindContent() { }
        #endregion

        #region IUIBoxView
        public void SetVisible(bool enable)
        {
            isVisible = enable;
            ApplyVisibility();
        }

        public void SetPointerInputEnabled(bool enable)
        {
            isPointerInputEnabled = enable;
            ApplyPointerInput();
        }

        public void SetDataSource(object setDataSource)
        {
            dataSource = setDataSource;
            ApplyDataSource();
        }

        public void SetBackExitAction(Action onBackExit)
        {
            backExitAction = onBackExit;
            ApplyBackExit();
        }

        public IUIChoice CreateChoiceOption(string text, int choiceOrder, Action onChoose)
        {
            var choiceEntryHandle = new ChoiceEntryHandle(this, text, true, onChoose);
            AddEntry(choiceEntryHandle);
            return choiceEntryHandle;
        }

        public void CreateChoiceSeparator(ChoiceSeparatorType separatorType) => AddEntry(new SeparatorEntryHandle(this, typeof(ChoiceEntryContainer), separatorType));
        #endregion

        #region EntryMethods
        public void AddEntry(EntryHandle entryHandle) // Note:  Public so boxes can add custom entries (e.g. game-side ChoiceHandles)
        {
            entries.Add(entryHandle);
            AttachElement(entryHandle);
        }

        internal void RemoveEntry(EntryHandle entryHandle)
        {
            entryHandle.DetachElement();
            entries.Remove(entryHandle);
            RefreshEmptyState(entryHandle.containerType);
        }

        public void ClearEntries()
        {
            foreach (EntryHandle entryHandle in entries) { entryHandle.Invalidate(); }
            entries.Clear();
            foreach (VisualElement container in containers.Values) { (container as ChoiceEntryContainer)?.RefreshEmptyState(); }
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

            // Early Return
            choiceEntryContainer = rootElement.Q<ChoiceEntryContainer>();
            if (!TryBindContent(rootElement)) { choiceEntryContainer = null; return; }

            // Setup root element + style
            boundRoot = rootElement;
            boundRoot.pickingMode = PickingMode.Ignore; // Only interactive children should intercept pointer input
            ApplyStyleSheets();
            
            // Setup pointer / click behaviour
            boundRoot.RegisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            backExitButton = boundRoot.Q<BackExitButton>();
            if (backExitButton != null) { backExitButton.clicked += HandleBackExitClicked; }

            // Setup view elements + initial state
            foreach (EntryHandle entryHandle in entries) { AttachElement(entryHandle); }
            ApplyVisibility();
            ApplyPointerInput();
            ApplyDataSource();
            ApplyBackExit();
        }

        private void UnbindRoot()
        {
            if (backExitButton != null) { backExitButton.clicked -= HandleBackExitClicked; }
            boundRoot?.UnregisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            foreach (EntryHandle entryHandle in entries) { entryHandle.DetachElement(); }
            UnbindContent();

            boundRoot = null;
            containers.Clear();
            choiceEntryContainer = null;
            backExitButton = null;
        }

        private void AttachElement(EntryHandle entryHandle)
        {
            // Note:  No-op until bound - entries created before the UI loads are attached in BindRoot
            if (boundRoot == null) { return; }
            GetEntryContainer(entryHandle.containerType)?.Add(entryHandle.BuildElement());
            RefreshEmptyState(entryHandle.containerType);
        }

        private VisualElement GetEntryContainer(Type containerType)
        {
            if (boundRoot == null) { return null; }
            if (containers.TryGetValue(containerType, out VisualElement container)) { return container; }

            container = boundRoot.Query<VisualElement>().Where(containerType.IsInstanceOfType).First();
            if (container == null) { Debug.LogWarning($"{GetType().Name}[{name}]:  UXML has no {containerType.Name} for entries."); }
            containers[containerType] = container;
            return container;
        }

        private void RefreshEmptyState(Type containerType)
        {
            if (GetEntryContainer(containerType) is ChoiceEntryContainer container) { container.RefreshEmptyState(); }
        }

        private void ApplyStyleSheets()
        {
            foreach (StyleSheet styleSheet in styleSheets)
            {
                if (styleSheet == null || boundRoot.styleSheets.Contains(styleSheet)) { continue; }
                boundRoot.styleSheets.Add(styleSheet);
            }
        }

        private void ApplyVisibility()
        {
            if (boundRoot == null) { return; }
            boundRoot.style.visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }

        private void ApplyPointerInput()
        {
            boundRoot?.EnableInClassList(USSClassNames.BoxView.pointerInputDisabled, !isPointerInputEnabled);
        }

        private void HandlePointerDown(PointerDownEvent pointerDownEvent)
        {
            // Note:  Stopped while trickling down from the root, so buttons never see the press (no click)
            if (!isPointerInputEnabled) { pointerDownEvent.StopImmediatePropagation(); }
        }

        private void ApplyDataSource()
        {
            if (boundRoot == null) { return; }
            boundRoot.dataSource = dataSource;
        }

        private void ApplyBackExit()
        {
            backExitButton?.SetShown(backExitAction != null);
        }

        private void HandleBackExitClicked() => backExitAction?.Invoke();
        #endregion
    }
}
