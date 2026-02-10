using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Frankie.ZoneManagement.UIEditor
{
    public class ZoneEditor : EditorWindow
    {
        // Tunables
        private Zone selectedZone;
        private const int _labelOffset = 130;
        private const int _nodePadding = 20;
        private const int _nodeBorder = 12;
        private const float _linkButtonMultiplier = 0.205f;
        private const float _addRemoveButtonMultiplier = 0.1f;
        private const float _connectionBezierOffsetMultiplier = 0.7f;
        private const float _connectionBezierWidth = 2f;
        private const string _backgroundName = "background";
        private const float _backgroundSize = 50f;

        // State
        [NonSerialized] private GUIStyle nodeStyle;
        [NonSerialized] private ZoneNode selectedNode;
        [NonSerialized] private bool draggable = false;
        [NonSerialized] private Vector2 draggingOffset;
        [NonSerialized] private ZoneNode creatingNode;
        [NonSerialized] private ZoneNode deletingNode;
        [NonSerialized] private ZoneNode linkingParentNode;
        [NonSerialized] private Tuple<ZoneNode, string> nodeIDUpdate = new(null, null);
        [NonSerialized] private Vector2 scrollPosition;
        [NonSerialized] private float scrollMaxX = 1;
        [NonSerialized] private float scrollMaxY = 1;

        [MenuItem("Window/Zone Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(ZoneEditor), false, "Zone Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var zone = EditorUtility.EntityIdToObject(instanceID) as Zone;
            if (zone == null) return false;
            zone.CreateRootNodeIfMissing();
            ShowEditorWindow();
            return true;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            SetupNodeStyle();
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

        private void OnSelectionChanged()
        {
            var newZone = Selection.activeObject as Zone;
            if (newZone == null) return;
            selectedZone = newZone;
            Repaint();
        }

        private void OnGUI()
        {
            if (selectedZone == null) { EditorGUILayout.LabelField("No zone selected."); }
            else
            {
                ProcessEvents();
                EditorGUILayout.LabelField(selectedZone.name);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawBackground();
                foreach (ZoneNode zoneNode in selectedZone.GetAllNodes())
                {
                    DrawConnections(zoneNode);
                }
                foreach (ZoneNode dialogueNode in selectedZone.GetAllNodes())
                {
                    DrawNode(dialogueNode);
                }
                EditorGUILayout.EndScrollView();

                if (deletingNode != null)
                {
                    selectedZone.DeleteNode(deletingNode);
                    deletingNode = null;
                }
                if (creatingNode != null)
                {
                    selectedZone.CreateChildNode(creatingNode);
                    creatingNode = null;
                }
                if (nodeIDUpdate.Item1 != null && nodeIDUpdate.Item2 != null)
                {
                    if (selectedZone.GetNodeFromID(nodeIDUpdate.Item2) == null)
                    {
                        string oldNodeID = nodeIDUpdate.Item1.GetNodeID();
                        if (nodeIDUpdate.Item1.SetNodeID(nodeIDUpdate.Item2))
                        {
                            selectedZone.UpdateNodeID(oldNodeID, nodeIDUpdate.Item2);
                        }
                        nodeIDUpdate = new Tuple<ZoneNode, string>(null, null);
                    }
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
            switch (Event.current.type)
            {
                case EventType.MouseDown:
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
                        Selection.activeObject = selectedNode;
                        draggingOffset = mousePosition + scrollPosition;
                    }

                    break;
                }
                case EventType.MouseDrag when selectedNode != null:
                {
                    if (draggable)
                    {
                        Vector2 currentMousePosition = Event.current.mousePosition;
                        selectedNode.SetPosition(currentMousePosition + draggingOffset);
                        GUI.changed = true;
                    }

                    break;
                }
                case EventType.MouseDrag when selectedNode == null:
                    scrollPosition = draggingOffset - Event.current.mousePosition;
                    GUI.changed = true;
                    break;
                case EventType.MouseUp:
                    selectedNode = null;
                    draggingOffset = new Vector2();
                    break;
            }
        }

        private void DrawNode(ZoneNode zoneNode)
        {
            GUILayout.BeginArea(zoneNode.GetRect(), nodeStyle);

            // Dragging Header
            GUILayout.BeginArea(zoneNode.GetDraggingRect(), nodeStyle);
            GUILayout.EndArea();

            // Node Properties
            EditorGUIUtility.labelWidth = _labelOffset;
            EditorGUILayout.LabelField("Unique ID:", zoneNode.name);
            EditorGUILayout.Space(_nodeBorder);

            // Detail
            EditorGUILayout.Space((float)_nodeBorder / 2, false);
            string oldID = zoneNode.GetNodeID();
            string newID = EditorGUILayout.TextField("Override ID:", oldID);
            if (oldID != newID)
            {
                nodeIDUpdate = new Tuple<ZoneNode, string>(zoneNode, newID);
            }

            // Additional Functionality
            GUILayout.FlexibleSpace();
            DrawLinkButtons(zoneNode);
            DrawAddRemoveButtons(zoneNode);
            GUILayout.EndArea();

            UpdateMaxScrollViewDimensions(zoneNode);
        }

        private void UpdateMaxScrollViewDimensions(ZoneNode zoneNode)
        {
            scrollMaxX = Mathf.Max(scrollMaxX, zoneNode.GetRect().xMax);
            scrollMaxY = Mathf.Max(scrollMaxY, zoneNode.GetRect().yMax);
        }

        private void DrawAddRemoveButtons(ZoneNode zoneNode)
        {
            // Set tags to create/delete at end of OnGUI to avoid operating on list while iterating over it
            GUILayout.BeginHorizontal();
            if (zoneNode != selectedZone.GetRootNode())
            {
                if (GUILayout.Button("-", GUILayout.Width(zoneNode.GetRect().width * _addRemoveButtonMultiplier)))
                {
                    deletingNode = zoneNode;
                }
            }

            if (GUILayout.Button("+", GUILayout.Width(zoneNode.GetRect().width * _addRemoveButtonMultiplier)))
            {
                creatingNode = zoneNode;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawLinkButtons(ZoneNode zoneNode)
        {
            if (linkingParentNode == null)
            {
                if (GUILayout.Button("link", GUILayout.Width(zoneNode.GetRect().width * _linkButtonMultiplier))) { linkingParentNode = zoneNode; }
            }
            else
            {
                if (zoneNode == linkingParentNode)
                {
                    if (GUILayout.Button("---", GUILayout.Width(zoneNode.GetRect().width * _linkButtonMultiplier))) { linkingParentNode = null; }
                }
                else
                {
                    string buttonText = "child";
                    if (selectedZone.IsRelated(linkingParentNode, zoneNode)) { buttonText = "unlink"; }

                    if (GUILayout.Button(buttonText, GUILayout.Width(zoneNode.GetRect().width * _linkButtonMultiplier)))
                    {
                        selectedZone.ToggleRelation(linkingParentNode, zoneNode);
                        linkingParentNode = null;
                    }
                }
            }
        }

        private void DrawConnections(ZoneNode zoneNode)
        {
            var startPoint = new Vector2(zoneNode.GetRect().xMax, zoneNode.GetRect().center.y);
            foreach (ZoneNode childNode in selectedZone.GetAllChildren(zoneNode))
            {
                var endPoint = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);
                float connectionBezierOffset = (endPoint.x - startPoint.x) * _connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.right * connectionBezierOffset, endPoint + Vector2.left * connectionBezierOffset,
                    Color.white, null, _connectionBezierWidth);
            }
        }

        private ZoneNode GetNodeAtPoint(Vector2 point, out bool getDraggable)
        {
            ZoneNode foundNode = null;
            getDraggable = false;
            foreach (ZoneNode zoneNode in selectedZone.GetAllNodes())
            {
                if (zoneNode.GetRect().Contains(point))
                {
                    foundNode = zoneNode;
                }

                var draggingRect = new Rect(zoneNode.GetRect().position, zoneNode.GetDraggingRect().size);
                if (draggingRect.Contains(point))
                {
                    getDraggable = true;
                }
            }
            return foundNode;
        }
    }
}
