using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Speech.UIEditor
{
    public class DialogueEditor : EditorWindow
    {
        // Tunables
        Dialogue selectedDialogue = null;
        static int labelOffset = 80;
        static int nodePadding = 20;
        static int nodeBorder = 12;
        static int textAreaHeight = 80;
        static float linkButtonMultiplier = 0.205f;
        static float addRemoveButtonMultiplier = 0.1f;
        static float connectionBezierOffsetMultiplier = 0.7f;
        static float connectionBezierWidth = 2f;
        const string backgroundName = "background";
        const float backgroundSize = 50f;

        // State
        [NonSerialized] GUIStyle nodeStyle = null;
        [NonSerialized] DialogueNode selectedNode = null;
        [NonSerialized] bool draggable = false;
        [NonSerialized] Vector2 draggingOffset = new Vector2();
        [NonSerialized] DialogueNode creatingNode = null;
        [NonSerialized] DialogueNode deletingNode = null;
        [NonSerialized] DialogueNode linkingParentNode = null;
        [NonSerialized] Vector2 scrollPosition = new Vector2();
        [NonSerialized] float scrollMaxX = 1;
        [NonSerialized] float scrollMaxY = 1;

        // Class States
        static string speakerNameToFill = "";

        [MenuItem("Window/Dialogue Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Dialogue dialogue = EditorUtility.InstanceIDToObject(instanceID) as Dialogue;
            if (dialogue != null)
            {
                dialogue.CreateRootNodeIfMissing();
                ShowEditorWindow();
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            SetupNodeStyle();
        }

        private void SetupNodeStyle()
        {
            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            nodeStyle.padding = new RectOffset(nodePadding, nodePadding / 2, nodePadding / 2, nodePadding);
            nodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);
        }

        private void ResetSpeakerNames()
        {
            speakerNameToFill = "";
        }

        private string SetupNodeSpeaker(DialogueNode dialogueNode, SpeakerType speaker, string speakerName)
        {
            speakerNameToFill = speakerName;

            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D; // Default behavior
            if (speaker == SpeakerType.playerSpeaker)
            {
                nodeStyle.normal.background = EditorGUIUtility.Load("node3") as Texture2D;
            }
            else if (speaker == SpeakerType.aiSpeaker)
            {
                List<CharacterProperties> activeSpeakers = selectedDialogue.GetActiveCharacters();
                if (activeSpeakers.Count > 0)
                {
                    for (int i = 0; i < activeSpeakers.Count; i++)
                    {
                        if (activeSpeakers[i] != dialogueNode.GetCharacterProperties()) { continue; }
                        if (i == 0) { nodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D; }
                        else if (i == 1) { nodeStyle.normal.background = EditorGUIUtility.Load("node2") as Texture2D; }
                        else if (i == 2) { nodeStyle.normal.background = EditorGUIUtility.Load("node5") as Texture2D; }
                        else if (i == 3) { nodeStyle.normal.background = EditorGUIUtility.Load("node6") as Texture2D; }
                    }
                }
            }
            else { nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D; }
            if (string.IsNullOrWhiteSpace(speakerNameToFill)) { speakerNameToFill = "Default"; }

            return speakerNameToFill;
        }

        private void OnSelectionChanged()
        {
            Dialogue newDialogue = Selection.activeObject as Dialogue;
            if (newDialogue != null)
            {
                selectedDialogue = newDialogue;
                ResetSpeakerNames();
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (selectedDialogue == null)
            {
                EditorGUILayout.LabelField("No dialogue selected.");
            }
            else
            {
                ProcessEvents();
                EditorGUILayout.LabelField(selectedDialogue.name);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawBackground();
                foreach (DialogueNode dialogueNode in selectedDialogue.GetAllNodes())
                {
                    DrawConnections(dialogueNode);
                }
                foreach (DialogueNode dialogueNode in selectedDialogue.GetAllNodes())
                {
                    DrawNode(dialogueNode);
                }
                EditorGUILayout.EndScrollView();

                if (deletingNode != null)
                {
                    selectedDialogue.DeleteNode(deletingNode);
                    deletingNode = null;
                }
                if (creatingNode != null)
                {
                    DialogueNode newChildNode = selectedDialogue.CreateChildNode(creatingNode);
                    creatingNode = null;
                }
            }
        }

        private void DrawBackground()
        {
            Rect canvas = GUILayoutUtility.GetRect(scrollMaxX, scrollMaxY);
            Texture2D backgroundTexture = Resources.Load(backgroundName) as Texture2D;
            GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, new Rect(0, 0, canvas.width / backgroundSize, canvas.height / backgroundSize));

            // Reset scrolling limits, to be updated after draw nodes
            scrollMaxX = 1f;
            scrollMaxX = 1f;
        }

        private void ProcessEvents()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                selectedNode = null;
                draggable = false;
                draggingOffset = new Vector2();

                Vector2 mousePosition = Event.current.mousePosition;

                selectedNode = GetNodeAtPoint(mousePosition + scrollPosition, out draggable);
                if (selectedNode != null)
                {
                    Selection.activeObject = selectedNode;
                    draggingOffset = selectedNode.GetPosition() - mousePosition;
                }
                else
                {
                    Selection.activeObject = selectedDialogue;
                    draggingOffset = mousePosition + scrollPosition;
                }
            }
            else if (Event.current.type == EventType.MouseDrag && selectedNode != null)
            {
                if (draggable)
                {
                    Vector2 currentMousePosition = Event.current.mousePosition;
                    selectedNode.SetPosition(currentMousePosition + draggingOffset);
                    GUI.changed = true;
                }
            }
            else if (Event.current.type == EventType.MouseDrag && selectedNode == null)
            {
                scrollPosition = draggingOffset - Event.current.mousePosition;
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                selectedNode = null;
                draggingOffset = new Vector2();
            }
        }

        private void DrawNode(DialogueNode dialogueNode)
        {
            SetupNodeSpeaker(dialogueNode, dialogueNode.GetSpeakerType(), dialogueNode.GetSpeakerName());
            GUILayout.BeginArea(dialogueNode.GetRect(), nodeStyle);

            // Dragging Header
            GUILayout.BeginArea(dialogueNode.GetDraggingRect(), nodeStyle);
            GUILayout.EndArea();

            // Node Properties
            EditorGUIUtility.labelWidth = labelOffset;
            EditorGUILayout.LabelField("Unique ID:", dialogueNode.name);
            EditorGUILayout.Space(nodeBorder);

            // Speaker Selection
            EditorGUILayout.BeginHorizontal();
            string newSpeakerName = EditorGUILayout.TextField("Speaker:", speakerNameToFill,
                GUILayout.Width((dialogueNode.GetRect().width - nodePadding * 2) / 2));
            bool speakerNameChanged = dialogueNode.SetSpeakerName(newSpeakerName);

            EditorGUILayout.Space(0f, true);
            Enum newSpeakerTypeEnum = EditorGUILayout.EnumPopup(dialogueNode.GetSpeakerType(),
                GUILayout.Width((dialogueNode.GetRect().width - nodePadding * 2) / 3));
            SpeakerType newSpeakerType = (SpeakerType)newSpeakerTypeEnum;
            if (newSpeakerType != dialogueNode.GetSpeakerType())
            {
                dialogueNode.SetSpeakerType(newSpeakerType);
                dialogueNode.SetSpeakerName(SetupNodeSpeaker(dialogueNode, newSpeakerType, ""));
            }

            if (newSpeakerType != SpeakerType.playerSpeaker && speakerNameChanged)
            {
                selectedDialogue.UpdateSpeakerName(dialogueNode.GetCharacterName(), newSpeakerName);
            }
            EditorGUILayout.Space(nodeBorder, false);
            EditorGUILayout.EndHorizontal();

            // Text Input
            EditorGUILayout.Space(nodeBorder / 2, false);
            EditorStyles.textField.wordWrap = true;
            string newText = EditorGUILayout.TextArea(dialogueNode.GetText(),
                GUILayout.Width(dialogueNode.GetRect().width - nodePadding * 2),
                GUILayout.Height(textAreaHeight));

            dialogueNode.SetText(newText);

            // Additional Functionality
            GUILayout.FlexibleSpace();
            DrawLinkButtons(dialogueNode);
            DrawAddRemoveButtons(dialogueNode);

            GUILayout.EndArea();

            UpdateMaxScrollViewDimensions(dialogueNode);
        }

        private void UpdateMaxScrollViewDimensions(DialogueNode dialogueNode)
        {
            scrollMaxX = Mathf.Max(scrollMaxX, dialogueNode.GetRect().xMax);
            scrollMaxY = Mathf.Max(scrollMaxY, dialogueNode.GetRect().yMax);
        }

        private void DrawAddRemoveButtons(DialogueNode dialogueNode)
        {
            // Set tags to create/delete at end of OnGUI to avoid operating on list while iterating over it
            GUILayout.BeginHorizontal();
            if (dialogueNode != selectedDialogue.GetRootNode())
            {
                if (GUILayout.Button("-", GUILayout.Width(dialogueNode.GetRect().width * addRemoveButtonMultiplier)))
                {
                    deletingNode = dialogueNode;
                }
            }

            if (GUILayout.Button("+", GUILayout.Width(dialogueNode.GetRect().width * addRemoveButtonMultiplier)))
            {
                creatingNode = dialogueNode;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawLinkButtons(DialogueNode dialogueNode)
        {
            if (linkingParentNode == null)
            {
                if (GUILayout.Button("link", GUILayout.Width(dialogueNode.GetRect().width * linkButtonMultiplier)))
                {
                    linkingParentNode = dialogueNode;
                }
            }
            else
            {
                if (dialogueNode != linkingParentNode)
                {
                    string buttonText = "child";
                    if (selectedDialogue.IsRelated(linkingParentNode, dialogueNode)) { buttonText = "unlink"; }

                    if (GUILayout.Button(buttonText, GUILayout.Width(dialogueNode.GetRect().width * linkButtonMultiplier)))
                    {
                        selectedDialogue.ToggleRelation(linkingParentNode, dialogueNode);
                        linkingParentNode = null;
                    }
                }
                else
                {
                    if (GUILayout.Button("---", GUILayout.Width(dialogueNode.GetRect().width * linkButtonMultiplier)))
                    {
                        linkingParentNode = null;
                    }
                }
            }
        }

        private void DrawConnections(DialogueNode dialogueNode)
        {
            Vector2 startPoint = new Vector2(dialogueNode.GetRect().xMax, dialogueNode.GetRect().center.y);
            foreach (DialogueNode childNode in selectedDialogue.GetAllChildren(dialogueNode))
            {
                Vector2 endPoint = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);
                float connectionBezierOffset = (endPoint.x - startPoint.x) * connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.right * connectionBezierOffset, endPoint + Vector2.left * connectionBezierOffset,
                    Color.white, null, connectionBezierWidth);
            }
        }

        private DialogueNode GetNodeAtPoint(Vector2 point, out bool draggable)
        {
            DialogueNode foundNode = null;
            draggable = false;
            foreach (DialogueNode dialogueNode in selectedDialogue.GetAllNodes())
            {
                if (dialogueNode.GetRect().Contains(point))
                {
                    foundNode = dialogueNode;
                }

                Rect draggingRect = new Rect(dialogueNode.GetRect().position, dialogueNode.GetDraggingRect().size);
                if (draggingRect.Contains(point))
                {
                    draggable = true;
                }
            }
            return foundNode;
        }
    }
}
