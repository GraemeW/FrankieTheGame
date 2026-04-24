using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Frankie.Speech.UIEditor
{
    [CustomEditor(typeof(Dialogue))]
    public class DialogueInspectorEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var headerLabel = new Label("Extreme Danger Zone (careful!)");
            
            var regenerateGUIDs = new Button { text = "Regenerate GUIDs" };
            regenerateGUIDs.RegisterCallback<ClickEvent>(RegenerateGUIDs);
            root.Add(regenerateGUIDs);
            
            var reserializeNodeDepthBreadth = new Button { text = "Reserialize Node Depth & Breadth" };
            reserializeNodeDepthBreadth.RegisterCallback<ClickEvent>(ReserializeNodeDepthBreadth);
            root.Add(reserializeNodeDepthBreadth);

            return root;
        }

        private void RegenerateGUIDs(ClickEvent clickEvent)
        {
            var dialogue = (Dialogue)target;
            if (dialogue == null)
            {
                return;
            }

            dialogue.RegenerateGUIDs();
        }

        private void ReserializeNodeDepthBreadth(ClickEvent clickEvent)
        {
            var dialogue = (Dialogue)target;
            if (dialogue == null)
            {
                return;
            }

            dialogue.ReserializeNodeDepthBreadth();
        }
    }
}
