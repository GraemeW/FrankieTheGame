using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using LowDefMustard.Utils.Editor;

namespace Frankie.Speech.Editor
{
    internal static class DialogueGraphWindowSkin
    {
        // Const Tunables
        internal const string windowClass = "dialogue-graph";
        private const string _blackboardOverlayID = "gtf-blackboard";

        public static void Apply(Graph graph)
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window is not IGraphWindow graphWindow || graphWindow.Graph != graph) { continue; }
                if (window.TryGetOverlay(_blackboardOverlayID, out Overlay blackboard) && blackboard.displayed) { blackboard.displayed = false; } // Kill blackboard, no BB variables in DialogueGraph
                
                DialogueVariableFieldHook.Register(window.rootVisualElement);
                if (!window.TryGetOverlay(DialogueVariablesOverlay.overlayID, out _)) { DialogueVariablesOverlay.AddTo(window); }
                if (window.rootVisualElement.ClassListContains(windowClass)) { continue; }

                DialogueGraphStyleSheets styleSheets = DialogueGraphStyleSheets.instance;
                StandardBrownTheme.Apply(window.rootVisualElement, styleSheets.GetThemeStyleSheet(), styleSheets.GetGraphStyleSheet());
                window.rootVisualElement.AddToClassList(windowClass);
            }
        }
    }
}
