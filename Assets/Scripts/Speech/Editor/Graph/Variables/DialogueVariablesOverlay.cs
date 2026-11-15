using UnityEditor;
using UnityEditor.Overlays;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Frankie.Speech.Editor
{
    internal sealed class DialogueVariablesOverlay : Overlay
    {
        // Note:  Overlays added from code (i.e. via DialogueGraphWindowSkin) take their type's full name as id
        //  - hence, not using [Overlay] ~ ignored on this path

        // Const Tunables
        [NoAutoStaticsCleanup] public static readonly string overlayID = typeof(DialogueVariablesOverlay).FullName;
        private const string _displayName = "Dialogue Variables";

        // Constructor
        private DialogueVariablesOverlay()
        {
            displayName = _displayName;
        }

        #region PublicMethods
        public static void AddTo(EditorWindow window)
        {
            var overlay = new DialogueVariablesOverlay();
            window.overlayCanvas.Add(overlay);

            // An overlay added from code reports displayed but stays hidden (display: none) until re-shown ~ re-show it once
            // Also, shown / hidden state isn't saved on this path (default is thus panel always opens shown)
            overlay.displayed = false;
            overlay.displayed = true;
        }

        public override void OnCreated()
        {
            collapsedChanged += OnCollapsedChanged;
            layoutChanged += OnLayoutChanged;
        }

        public override void OnWillBeDestroyed()
        {
            collapsedChanged -= OnCollapsedChanged;
            layoutChanged -= OnLayoutChanged;
        }

        public override VisualElement CreatePanelContent()
        {
            ClearTooltip();
            return DialogueVariablesPanel.CreateOverlay(containerWindow != null ? containerWindow.rootVisualElement : null);
        }
        #endregion

        #region PrivateMethods
        private void ClearTooltip() => rootVisualElement.tooltip = ""; // Otherwise, overlay default behaviour copies "displayName" onto its root as a hover tooltip
        private void OnCollapsedChanged(bool _) => ClearTooltip();
        private void OnLayoutChanged(Layout _) => ClearTooltip();
        #endregion
    }
}
