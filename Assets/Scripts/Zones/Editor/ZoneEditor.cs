using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Frankie.Zone.Editor
{
    public class ZoneEditor : EditorWindow
    {
        // Tunables
        Zone selectedZone = null;
        static int labelOffset = 130;
        static int nodePadding = 20;
        static int nodeBorder = 12;
        static float linkButtonMultiplier = 0.205f;
        static float addRemoveButtonMultiplier = 0.1f;
        static float connectionBezierOffsetMultiplier = 0.7f;
        static float connectionBezierWidth = 2f;
        const string backgroundName = "background";
        const float backgroundSize = 50f;

        // State
        [NonSerialized] GUIStyle nodeStyle = null;
        [NonSerialized] ZoneNode selectedNode = null;
        [NonSerialized] bool draggable = false;
        [NonSerialized] Vector2 draggingOffset = new Vector2();
        [NonSerialized] ZoneNode creatingNode = null;
        [NonSerialized] ZoneNode deletingNode = null;
        [NonSerialized] ZoneNode linkingParentNode = null;
        [NonSerialized] Vector2 scrollPosition = new Vector2();
        [NonSerialized] float scrollMaxX = 1;
        [NonSerialized] float scrollMaxY = 1;

        [MenuItem("Window/Zone Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(ZoneEditor), false, "Zone Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Zone zone = EditorUtility.InstanceIDToObject(instanceID) as Zone;
            if (zone != null)
            {
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

        private void OnSelectionChanged()
        {
            Zone newZone = Selection.activeObject as Zone;
            if (newZone != null)
            {
                selectedZone = newZone;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (selectedZone == null)
            {
                EditorGUILayout.LabelField("No zone selected.");
            }
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
                    ZoneNode newChildNode = selectedZone.CreateChildNode(creatingNode);
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
                    Selection.activeObject = selectedNode;
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

        private void DrawNode(ZoneNode zoneNode)
        {
            GUILayout.BeginArea(zoneNode.GetRect(), nodeStyle);

            // Dragging Header
            GUILayout.BeginArea(zoneNode.GetDraggingRect(), nodeStyle);
            GUILayout.EndArea();

            // Node Properties
            EditorGUIUtility.labelWidth = labelOffset;
            EditorGUILayout.LabelField("Unique ID:", zoneNode.name);
            EditorGUILayout.Space(nodeBorder);

            // Detail
            EditorGUILayout.Space(nodeBorder / 2, false);
            string newDetail = EditorGUILayout.TextField("Node Detail:", zoneNode.GetDetail());
            zoneNode.SetDetail(newDetail);

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
                if (GUILayout.Button("-", GUILayout.Width(zoneNode.GetRect().width * addRemoveButtonMultiplier)))
                {
                    deletingNode = zoneNode;
                }
            }

            if (GUILayout.Button("+", GUILayout.Width(zoneNode.GetRect().width * addRemoveButtonMultiplier)))
            {
                creatingNode = zoneNode;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawLinkButtons(ZoneNode zoneNode)
        {
            if (linkingParentNode == null)
            {
                if (GUILayout.Button("link", GUILayout.Width(zoneNode.GetRect().width * linkButtonMultiplier)))
                {
                    linkingParentNode = zoneNode;
                }
            }
            else
            {
                if (zoneNode != linkingParentNode)
                {
                    string buttonText = "child";
                    if (selectedZone.IsRelated(linkingParentNode, zoneNode)) { buttonText = "unlink"; }

                    if (GUILayout.Button(buttonText, GUILayout.Width(zoneNode.GetRect().width * linkButtonMultiplier)))
                    {
                        selectedZone.ToggleRelation(linkingParentNode, zoneNode);
                        linkingParentNode = null;
                    }
                }
                else
                {
                    if (GUILayout.Button("---", GUILayout.Width(zoneNode.GetRect().width * linkButtonMultiplier)))
                    {
                        linkingParentNode = null;
                    }
                }
            }
        }

        private void DrawConnections(ZoneNode zoneNode)
        {
            Vector2 startPoint = new Vector2(zoneNode.GetRect().xMax, zoneNode.GetRect().center.y);
            foreach (ZoneNode childNode in selectedZone.GetAllChildren(zoneNode))
            {
                Vector2 endPoint = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);
                float connectionBezierOffset = (endPoint.x - startPoint.x) * connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.right * connectionBezierOffset, endPoint + Vector2.left * connectionBezierOffset,
                    Color.white, null, connectionBezierWidth);
            }
        }

        private ZoneNode GetNodeAtPoint(Vector2 point, out bool draggable)
        {
            ZoneNode foundNode = null;
            draggable = false;
            foreach (ZoneNode zoneNode in selectedZone.GetAllNodes())
            {
                if (zoneNode.GetRect().Contains(point))
                {
                    foundNode = zoneNode;
                }

                Rect draggingRect = new Rect(zoneNode.GetRect().position, zoneNode.GetDraggingRect().size);
                if (draggingRect.Contains(point))
                {
                    draggable = true;
                }
            }
            return foundNode;
        }
    }
}