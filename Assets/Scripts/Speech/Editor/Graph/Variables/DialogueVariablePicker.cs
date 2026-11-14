using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils.Editor;

namespace Frankie.Speech.Editor
{
    internal sealed class DialogueVariablePicker : PopupWindowContent
    {
        // Const Tunables
        [NoAutoStaticsCleanup] private static readonly Vector2 _windowSize = new(300f, 400f);

        // State
        private readonly Action<ScriptableObject> onPicked;
        private readonly float activatorWidth;

        // Constructor
        internal DialogueVariablePicker(Action<ScriptableObject> onPicked, float activatorWidth)
        {
            this.onPicked = onPicked;
            this.activatorWidth = activatorWidth;
        }

        #region PublicMethods
        public static void Show(Rect activatorRect, Action<ScriptableObject> onPicked) => UnityEditor.PopupWindow.Show(activatorRect, new DialogueVariablePicker(onPicked, activatorRect.width));

        public override VisualElement CreateGUI()
        {
            Vector2 windowSize = GetWindowSize(); // Fixed size ~ popup otherwise fits itself to content (grows w/ every unfolded group)
            var root = new VisualElement { style = { width = windowSize.x, height = windowSize.y, flexGrow = 0f, flexShrink = 0f } };
            DialogueGraphStyleSheets styleSheets = DialogueGraphStyleSheets.instance;
            StandardBrownTheme.Apply(root, styleSheets.GetThemeStyleSheet(), styleSheets.GetGraphStyleSheet());
            root.AddToClassList(DialogueGraphWindowSkin.windowClass);

            DialogueVariablesPanel panel = DialogueVariablesPanel.CreatePicker(Pick);
            panel.style.height = Length.Percent(100f);
            root.Add(panel);
            return root;
        }
        
        public override Vector2 GetWindowSize() => new(Mathf.Max(_windowSize.x, activatorWidth), _windowSize.y);
        #endregion

        #region PrivateMethods
        private void Pick(ScriptableObject variable)
        {
            onPicked?.Invoke(variable);
            if (editorWindow != null) { editorWindow.Close(); }
        }
        #endregion
    }
}
