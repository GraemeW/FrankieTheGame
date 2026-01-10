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
            
            var regenerateGUIDs = new Button { text = "Regenerate GUIDs" };
            regenerateGUIDs.RegisterCallback<ClickEvent>(RegenerateGUIDs);
            
            root.Add(regenerateGUIDs);
            return root;
        }

        private void RegenerateGUIDs(ClickEvent clickEvent)
        {
            var dialogue = (Dialogue)target;
            if (dialogue == null) { return ; }
            
            dialogue.RegenerateGUIDs();
        }
    }
}
