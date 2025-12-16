using System;
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
        private Dialogue selectedDialogue;
        private const int _labelOffset = 80;
        private const int _nodePadding = 20;
        private const int _nodeBorder = 12;
        private const int _textAreaHeight = 80;
        private const float _linkButtonMultiplier = 0.205f;
        private const float _addRemoveButtonMultiplier = 0.1f;
        private const float _connectionBezierOffsetMultiplier = 0.7f;
        private const float _connectionBezierWidth = 2f;
        private const string _backgroundName = "background";
        private const float _backgroundSize = 50f;

        // State
        [NonSerialized] private GUIStyle nodeStyle;
        [NonSerialized] private DialogueNode selectedNode;
        [NonSerialized] private bool draggable = false;
        [NonSerialized] private Vector2 draggingOffset;
        [NonSerialized] private DialogueNode creatingNode;
        [NonSerialized] private DialogueNode deletingNode;
        [NonSerialized] private DialogueNode linkingParentNode;
        [NonSerialized] private Vector2 scrollPosition;
        [NonSerialized] private float scrollMaxX = 1;
        [NonSerialized] private float scrollMaxY = 1;

        // Class States
        private string speakerNameToFill = "";
        
        #region UnityMethods
        [MenuItem("Window/Dialogue Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var dialogue = EditorUtility.EntityIdToObject(instanceID) as Dialogue;
            if (dialogue == null) return false;
            dialogue.CreateRootNodeIfMissing();
            ShowEditorWindow();
            return true;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            SetupNodeStyle();
        }
        #endregion

        private void ResetSpeakerNames()
        {
            speakerNameToFill = "";
        }
        
        private void SetupNodeStyle()
        {
            nodeStyle = new GUIStyle
            {
                normal = { background = EditorGUIUtility.Load("node0") as Texture2D },
                padding = new RectOffset(_nodePadding, _nodePadding / 2, _nodePadding / 2, _nodePadding),
                border = new RectOffset(_nodeBorder, _nodeBorder, _nodeBorder, _nodeBorder)
            };
        }

        private string SetupNodeSpeaker(DialogueNode dialogueNode, SpeakerType speaker, string speakerName)
        {
            speakerNameToFill = speakerName;

            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D; // Default behavior
            switch (speaker)
            {
                case SpeakerType.PlayerSpeaker:
                    nodeStyle.normal.background = EditorGUIUtility.Load("node3") as Texture2D;
                    break;
                case SpeakerType.AISpeaker:
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

                    break;
                }
                case SpeakerType.NarratorDirection:
                default:
                    nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
                    break;
            }
            if (string.IsNullOrWhiteSpace(speakerNameToFill)) { speakerNameToFill = "Default"; }

            return speakerNameToFill;
        }

        private void OnSelectionChanged()
        {
            var newDialogue = Selection.activeObject as Dialogue;
            if (newDialogue == null) return;
            selectedDialogue = newDialogue;
            ResetSpeakerNames();
            Repaint();
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
                    selectedDialogue.CreateChildNode(creatingNode);
                    creatingNode = null;
                }
            }
        }

        private void DrawBackground()
        {
            Rect canvas = GUILayoutUtility.GetRect(scrollMaxX, scrollMaxY);
            var backgroundTexture = Resources.Load(_backgroundName) as Texture2D;
            if (backgroundTexture == null) { return; }
            GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, new Rect(0, 0, canvas.width / _backgroundSize, canvas.height / _backgroundSize));

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
            EditorGUIUtility.labelWidth = _labelOffset;
            EditorGUILayout.LabelField("Unique ID:", dialogueNode.name);
            EditorGUILayout.Space(_nodeBorder);

            // Speaker Selection
            EditorGUILayout.BeginHorizontal();
            string newSpeakerName = EditorGUILayout.TextField("Speaker:", speakerNameToFill,
                GUILayout.Width((dialogueNode.GetRect().width - _nodePadding * 2) / 2));
            bool speakerNameChanged = dialogueNode.SetSpeakerName(newSpeakerName);

            EditorGUILayout.Space(0f, true);
            Enum newSpeakerTypeEnum = EditorGUILayout.EnumPopup(dialogueNode.GetSpeakerType(),
                GUILayout.Width((dialogueNode.GetRect().width - _nodePadding * 2) / 3));
            SpeakerType newSpeakerType = (SpeakerType)newSpeakerTypeEnum;
            if (newSpeakerType != dialogueNode.GetSpeakerType())
            {
                dialogueNode.SetSpeakerType(newSpeakerType);
                dialogueNode.SetSpeakerName(SetupNodeSpeaker(dialogueNode, newSpeakerType, ""));
            }

            if (newSpeakerType != SpeakerType.PlayerSpeaker && speakerNameChanged)
            {
                selectedDialogue.UpdateSpeakerName(dialogueNode.GetCharacterName(), newSpeakerName);
            }
            EditorGUILayout.Space(_nodeBorder, false);
            EditorGUILayout.EndHorizontal();

            // Text Input
            EditorGUILayout.Space((float)_nodeBorder / 2, false);
            EditorStyles.textField.wordWrap = true;
            string newText = EditorGUILayout.TextArea(dialogueNode.GetText(),
                GUILayout.Width(dialogueNode.GetRect().width - _nodePadding * 2),
                GUILayout.Height(_textAreaHeight));

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
                if (GUILayout.Button("-", GUILayout.Width(dialogueNode.GetRect().width * _addRemoveButtonMultiplier))) { deletingNode = dialogueNode; }
            }

            if (GUILayout.Button("+", GUILayout.Width(dialogueNode.GetRect().width * _addRemoveButtonMultiplier))) { creatingNode = dialogueNode; }
            GUILayout.EndHorizontal();
        }

        private void DrawLinkButtons(DialogueNode dialogueNode)
        {
            if (linkingParentNode == null)
            {
                if (GUILayout.Button("link", GUILayout.Width(dialogueNode.GetRect().width * _linkButtonMultiplier))) { linkingParentNode = dialogueNode; }
            }
            else
            {
                if (dialogueNode == linkingParentNode)
                {
                    if (GUILayout.Button("---", GUILayout.Width(dialogueNode.GetRect().width * _linkButtonMultiplier))) { linkingParentNode = null; }
                }
                else
                {
                    string buttonText = "child";
                    if (Dialogue.IsRelated(linkingParentNode, dialogueNode)) { buttonText = "unlink"; }

                    if (!GUILayout.Button(buttonText, GUILayout.Width(dialogueNode.GetRect().width * _linkButtonMultiplier))) return;
                    selectedDialogue.ToggleRelation(linkingParentNode, dialogueNode);
                    linkingParentNode = null;
                }
            }
        }

        private void DrawConnections(DialogueNode dialogueNode)
        {
            var startPoint = new Vector2(dialogueNode.GetRect().xMax, dialogueNode.GetRect().center.y);
            foreach (DialogueNode childNode in selectedDialogue.GetAllChildren(dialogueNode))
            {
                var endPoint = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);
                float connectionBezierOffset = (endPoint.x - startPoint.x) * _connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.right * connectionBezierOffset, endPoint + Vector2.left * connectionBezierOffset,
                    Color.white, null, _connectionBezierWidth);
            }
        }

        private DialogueNode GetNodeAtPoint(Vector2 point, out bool getDraggable)
        {
            DialogueNode foundNode = null;
            getDraggable = false;
            foreach (DialogueNode dialogueNode in selectedDialogue.GetAllNodes())
            {
                if (dialogueNode.GetRect().Contains(point)) { foundNode = dialogueNode; }
                var draggingRect = new Rect(dialogueNode.GetRect().position, dialogueNode.GetDraggingRect().size);
                if (draggingRect.Contains(point)) { getDraggable = true; }
            }
            return foundNode;
        }
    }
}
