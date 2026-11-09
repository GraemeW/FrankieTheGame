using UnityEditor;
using UnityEditor.Toolbars;
using Unity.GraphToolkit.Editor;

namespace Frankie.Speech.Editor
{
    [GraphToolbarElement(id, typeof(DialogueGraph))]
    internal sealed class DialogueGraphRekeyButton : EditorToolbarButton, IAccessContainerWindow
    {
        // Const Tunables
        public const string id = "DialogueGraph/RekeyLines";

        // State
        public EditorWindow containerWindow { get; set; }

        public DialogueGraphRekeyButton()
        {
            text = "Re-key Lines";
            tooltip = "Renumber every line's localization key (dialogue.depth.breadth) breadth-first from Entry - translations move with their lines";
            clicked += HandleClicked;
        }

        private void HandleClicked()
        {
            if (containerWindow is IGraphWindow { Graph: DialogueGraph graph }) { DialogueGraphLocalizationSync.RekeyLines(graph); }
        }
    }
}
