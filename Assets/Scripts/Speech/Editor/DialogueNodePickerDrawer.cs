using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Speech.Editor
{
    [CustomPropertyDrawer(typeof(DialogueNodePickerAttribute))]
    internal sealed class DialogueNodePickerDrawer : PropertyDrawer
    {
        // Const Tunables
        private const string _pickLabel = "Pick";
        private const string _pickTooltip = "Choose a dialogue line by its text";
        private const string _allDialoguesMenu = "All dialogues/";
        private const string _conversantDialogueProperty = "dialogue";

        #region UnityMethods
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var objectField = new ObjectField(property.displayName) { objectType = typeof(DialogueNode), allowSceneObjects = false, style = { flexGrow = 1, flexShrink = 1 } };
            objectField.AddToClassList(BaseField<Object>.alignedFieldUssClassName);
            objectField.BindProperty(property);

            SerializedObject serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath;
            var pickButton = new Button(() => ShowMenu(serializedObject, propertyPath)) { text = _pickLabel, tooltip = _pickTooltip };

            row.Add(objectField);
            row.Add(pickButton);
            return row;
        }
        #endregion

        #region PrivateMethods
        private static void ShowMenu(SerializedObject serializedObject, string propertyPath)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            var current = property.objectReferenceValue as DialogueNode;
            Object[] targets = serializedObject.targetObjects; // Menu callbacks run later: the inspector's SerializedObject may be disposed by then
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), current == null, () => Assign(targets, propertyPath, null));

            if (TryGetContextDialogue(current, serializedObject.targetObject, out Dialogue contextDialogue))
            {
                menu.AddSeparator("");
                AddDialogueItems(menu, contextDialogue, $"{contextDialogue.name}/", current, targets, propertyPath);
            }

            menu.AddSeparator("");
            foreach (Dialogue dialogue in FindAllDialogues()) { AddDialogueItems(menu, dialogue, $"{_allDialoguesMenu}{dialogue.name}/", current, targets, propertyPath); }
            menu.ShowAsContext();
        }

        private static bool TryGetContextDialogue(DialogueNode current, Object target, out Dialogue contextDialogue)
        {
            contextDialogue = null;
            if (current != null && AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(current)) is Dialogue owningDialogue) { contextDialogue = owningDialogue; }
            else if (target is Component component)
            {
                AIConversant conversant = component.GetComponent<AIConversant>(); // Triggers live with their conversant
                if (conversant != null) { contextDialogue = new SerializedObject(conversant).FindProperty(_conversantDialogueProperty)?.objectReferenceValue as Dialogue; }
            }
            return contextDialogue != null;
        }

        private static IEnumerable<Dialogue> FindAllDialogues()
        {
            return AssetDatabase.FindAssets($"t:{nameof(Dialogue)}").Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadMainAssetAtPath(path) as Dialogue).Where(dialogue => dialogue != null).OrderBy(dialogue => dialogue.name);
        }

        private static void AddDialogueItems(GenericMenu menu, Dialogue dialogue, string menuPrefix, DialogueNode current, Object[] targets, string propertyPath)
        {
            if (dialogue == null) { return; }
            
            var usedLabels = new HashSet<string>();
            foreach (DialogueNode node in dialogue.GetAllDialogueNodes().Where(node => node != null).OrderBy(node => node.GetNodeDepth()).ThenBy(node => node.GetNodeBreadth()))
            {
                string label = node.name; // Imported nodes are named by their label (DialogueNodeLabels)
                for (int copy = 2; !usedLabels.Add(label); copy++) { label = $"{node.name} ({copy})"; } // GenericMenu merges identical paths
                menu.AddItem(new GUIContent(menuPrefix + label), node == current, () => Assign(targets, propertyPath, node));
            }
        }

        private static void Assign(Object[] targets, string propertyPath, DialogueNode node)
        {
            // Fresh SerializedObject over the captured targets (undo-recorded, multi-object aware) && skipped if a target was destroyed meanwhile
            if (targets == null || targets.Length == 0 || targets.Any(target => target == null)) { return; }
            using var serializedObject = new SerializedObject(targets);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            if (property == null) { return; }
            
            property.objectReferenceValue = node;
            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}
