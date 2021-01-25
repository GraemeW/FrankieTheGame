using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Frankie.Combat.Editor
{
    public class SkillTreeEditor : EditorWindow
    {
        // Tunables
        SkillTree selectedSkillTree = null;
        static int branchPadding = 20;
        static int branchBorder = 12;
        static float addRemoveButtonMultiplier = 0.1f;
        static float connectionBezierOffsetMultiplier = 0.7f;
        static float connectionBezierWidth = 2f;
        const string backgroundName = "background";
        const float backgroundSize = 50f;

        // State
        [NonSerialized] GUIStyle skillBranchStyle = null;
        [NonSerialized] GUIStyle rootSkillBranchStyle = null;
        [NonSerialized] SkillBranch selectedSkillBranch = null;
        [NonSerialized] bool draggable = false;
        [NonSerialized] Vector2 draggingOffset = new Vector2();
        [NonSerialized] SkillBranch creatingSkillBranch = null;
        [NonSerialized] SkillBranchMapping creatingSkillBranchPosition = default;
        [NonSerialized] SkillBranch deletingSkillBranch = null;
        [NonSerialized] SkillBranchMapping deletingSkillBranchMapping = default;
        [NonSerialized] Vector2 scrollPosition = new Vector2();

        [NonSerialized] float scrollMinX = 1;
        [NonSerialized] float scrollMinY = 1;
        [NonSerialized] float scrollMaxX = 1;
        [NonSerialized] float scrollMaxY = 1;

        [MenuItem("Window/SkillTree Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(SkillTreeEditor), false, "SkillTree Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            SkillTree zone = EditorUtility.InstanceIDToObject(instanceID) as SkillTree;
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
            SetupSkillBranchStyle();
        }

        private void SetupSkillBranchStyle()
        {
            skillBranchStyle = new GUIStyle();
            skillBranchStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            skillBranchStyle.padding = new RectOffset(branchPadding, branchPadding / 2, branchPadding / 2, branchPadding);
            skillBranchStyle.border = new RectOffset(branchBorder, branchBorder, branchBorder, branchBorder);

            rootSkillBranchStyle = new GUIStyle();
            rootSkillBranchStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
            rootSkillBranchStyle.padding = new RectOffset(branchPadding, branchPadding / 2, branchPadding / 2, branchPadding);
            rootSkillBranchStyle.border = new RectOffset(branchBorder, branchBorder, branchBorder, branchBorder);
        }

        private void OnSelectionChanged()
        {
            SkillTree newSkillTree = Selection.activeObject as SkillTree;
            if (newSkillTree != null)
            {
                selectedSkillTree = newSkillTree;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (selectedSkillTree == null)
            {
                EditorGUILayout.LabelField("No zone selected.");
            }
            else
            {
                ProcessEvents();
                EditorGUILayout.LabelField(selectedSkillTree.name);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawBackground();
                foreach (SkillBranch skillBranch in selectedSkillTree.GetAllBranches())
                {
                    DrawConnections(skillBranch);
                }
                foreach (SkillBranch skillBranch in selectedSkillTree.GetAllBranches())
                {
                    DrawBranch(skillBranch);
                }
                EditorGUILayout.EndScrollView();

                if (deletingSkillBranch != null)
                {
                    selectedSkillTree.DeleteSkillBranch(deletingSkillBranch, deletingSkillBranchMapping);
                    deletingSkillBranch = null;
                    deletingSkillBranchMapping = default;
                }
                if (creatingSkillBranch != null)
                {
                    SkillBranch newChildSkillBranch = selectedSkillTree.CreateChildSkillBranch(creatingSkillBranch, creatingSkillBranchPosition);
                    creatingSkillBranch = null;
                    creatingSkillBranchPosition = default;
                }
            }
        }

        private void DrawBackground()
        {
            Rect canvas = GUILayoutUtility.GetRect(scrollMaxX, scrollMaxY);
            Texture2D backgroundTexture = Resources.Load(backgroundName) as Texture2D;
            GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, new Rect(0, 0, canvas.width / backgroundSize, canvas.height / backgroundSize));
            ResetOriginForNegativeBranchPositions();

            // Reset scrolling limits, to be updated after draw nodes
            scrollMaxX = 1f;
            scrollMaxX = 1f;
            scrollMinX = 1f;
            scrollMinY = 1f;
        }

        private void ProcessEvents()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                selectedSkillBranch = null;
                draggable = false;
                draggingOffset = new Vector2();

                Vector2 mousePosition = Event.current.mousePosition;

                selectedSkillBranch = GetSkillBranchAtPoint(mousePosition + scrollPosition, out draggable);
                if (selectedSkillBranch != null)
                {
                    Selection.activeObject = selectedSkillBranch;
                    draggingOffset = selectedSkillBranch.GetPosition() - mousePosition;
                }
                else
                {
                    Selection.activeObject = selectedSkillBranch;
                    draggingOffset = mousePosition + scrollPosition;
                }
            }
            else if (Event.current.type == EventType.MouseDrag && selectedSkillBranch != null)
            {
                if (draggable)
                {
                    Vector2 currentMousePosition = Event.current.mousePosition;
                    selectedSkillBranch.SetPosition(currentMousePosition + draggingOffset);
                    GUI.changed = true;
                }
            }
            else if (Event.current.type == EventType.MouseDrag && selectedSkillBranch == null)
            {
                scrollPosition = draggingOffset - Event.current.mousePosition;
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                selectedSkillBranch = null;
                draggingOffset = new Vector2();
            }
        }

        private void DrawBranch(SkillBranch skillBranch)
        {
            GUIStyle currentStyle = skillBranchStyle;
            if (selectedSkillTree.GetRootSkillBranch() == skillBranch)
            {
                currentStyle = rootSkillBranchStyle;
            }

            GUILayout.BeginArea(skillBranch.GetRect(), currentStyle);

            // Dragging Header
            GUILayout.BeginArea(skillBranch.GetDraggingRect(), currentStyle);
            DrawBranchHeader(skillBranch);
            GUILayout.EndArea();

            // Additional Functionality
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            DrawBranchDetail(skillBranch, SkillBranchMapping.up);
            DrawBranchDetail(skillBranch, SkillBranchMapping.left);
            DrawBranchDetail(skillBranch, SkillBranchMapping.right);
            DrawBranchDetail(skillBranch, SkillBranchMapping.down);
            GUILayout.EndVertical();
            GUILayout.EndArea();

            UpdateMaxScrollViewDimensions(skillBranch);
        }

        private void UpdateMaxScrollViewDimensions(SkillBranch skillBranch)
        {
            scrollMaxX = Mathf.Max(scrollMaxX, skillBranch.GetRect().xMax);
            scrollMaxY = Mathf.Max(scrollMaxY, skillBranch.GetRect().yMax);
            scrollMinX = Mathf.Min(scrollMinX, skillBranch.GetRect().xMin);
            scrollMinY = Mathf.Min(scrollMinY, skillBranch.GetRect().yMin);
        }

        private void ResetOriginForNegativeBranchPositions()
        {
            foreach (SkillBranch skillBranch in selectedSkillTree.GetAllBranches())
            {
                if (scrollMinX < 0 || scrollMinY < 0)
                {
                    Vector2 currentPosition = skillBranch.GetPosition();
                    Vector2 shiftedPosition = new Vector2(currentPosition.x - scrollMinX, currentPosition.y - scrollMinY);
                    skillBranch.SetPosition(shiftedPosition);
                }
            }
        }

        private void DrawBranchHeader(SkillBranch skillBranch)
        {
            GUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 120;
            EditorGUILayout.LabelField("Branch Available");
            GUILayout.FlexibleSpace();
            DrawBranchRemoveButtons(skillBranch);
            GUILayout.EndHorizontal();
        }

        private void DrawBranchDetail(SkillBranch skillBranch, SkillBranchMapping skillBranchMapping)
        {
            GUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            EditorGUILayout.LabelField(skillBranchMapping.ToString(), GUILayout.Height(22));

            string equippedSkillName = "No Skill";
            if (skillBranch.GetSkill(skillBranchMapping) != null) { equippedSkillName = skillBranch.GetSkill(skillBranchMapping).name; }
            string newSkillName = EditorGUILayout.TextField(equippedSkillName, GUILayout.Width(110));
            if (!string.IsNullOrWhiteSpace(newSkillName) && newSkillName != "No Skill")
            {
                skillBranch.SetSkill(newSkillName, skillBranchMapping);
            }
            
            GUILayout.FlexibleSpace();
            DrawBranchAddButtons(skillBranch, skillBranchMapping);
            GUILayout.EndHorizontal();
        }

        private void DrawBranchRemoveButtons(SkillBranch skillBranch)
        {
            // Set tags to create/delete at end of OnGUI to avoid operating on list while iterating over it
            if (skillBranch != selectedSkillTree.GetRootSkillBranch())
            {
                if (GUILayout.Button("-", GUILayout.Width(skillBranch.GetRect().width * addRemoveButtonMultiplier)))
                {
                    deletingSkillBranch = skillBranch;
                    deletingSkillBranchMapping = skillBranch.GetParentBranchMapping();
                }
            }
        }

        private void DrawBranchAddButtons(SkillBranch skillBranch, SkillBranchMapping skillBranchMapping)
        {
            // Set tags to create/delete at end of OnGUI to avoid operating on list while iterating over it
            if (!skillBranch.HasBranch(skillBranchMapping) && skillBranch.HasSkill(skillBranchMapping)) // only allow branch if skill populated
            {
                if (GUILayout.Button("+", GUILayout.Width(skillBranch.GetRect().width * addRemoveButtonMultiplier)))
                {
                    creatingSkillBranch = skillBranch;
                    creatingSkillBranchPosition = skillBranchMapping;
                }
            }
        }

        private void DrawConnections(SkillBranch skillBranch)
        {
            SkillBranch upSkillBranch = selectedSkillTree.GetChildSkillBranch(skillBranch, SkillBranchMapping.up);
            SkillBranch leftSkillBranch = selectedSkillTree.GetChildSkillBranch(skillBranch, SkillBranchMapping.left);
            SkillBranch rightSkillBranch = selectedSkillTree.GetChildSkillBranch(skillBranch, SkillBranchMapping.right);
            SkillBranch downSkillBranch = selectedSkillTree.GetChildSkillBranch(skillBranch, SkillBranchMapping.down);

            if (upSkillBranch != null)
            {
                Vector2 startPoint = new Vector2(skillBranch.GetRect().center.x, skillBranch.GetRect().yMin);
                Vector2 endPoint = new Vector2(upSkillBranch.GetRect().center.x, upSkillBranch.GetRect().yMax);
                float connectionBezierOffset = (endPoint.y - startPoint.y) * connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.up * connectionBezierOffset, endPoint + Vector2.down * connectionBezierOffset,
                    Color.white, null, connectionBezierWidth);
            }
            if (leftSkillBranch != null)
            {
                Vector2 startPoint = new Vector2(skillBranch.GetRect().xMin, skillBranch.GetRect().center.y);
                Vector2 endPoint = new Vector2(leftSkillBranch.GetRect().xMax, leftSkillBranch.GetRect().center.y);
                float connectionBezierOffset = (startPoint.x - endPoint.x) * connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.left * connectionBezierOffset, endPoint + Vector2.right * connectionBezierOffset,
                    Color.white, null, connectionBezierWidth);
            }
            if (rightSkillBranch != null)
            {
                Vector2 startPoint = new Vector2(skillBranch.GetRect().xMax, skillBranch.GetRect().center.y);
                Vector2 endPoint = new Vector2(rightSkillBranch.GetRect().xMin, rightSkillBranch.GetRect().center.y);
                float connectionBezierOffset = (endPoint.x - startPoint.x) * connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.right * connectionBezierOffset, endPoint + Vector2.left * connectionBezierOffset,
                    Color.white, null, connectionBezierWidth);
            }
            if (downSkillBranch != null)
            {
                Vector2 startPoint = new Vector2(skillBranch.GetRect().center.x, skillBranch.GetRect().yMax);
                Vector2 endPoint = new Vector2(downSkillBranch.GetRect().center.x, downSkillBranch.GetRect().yMin);
                float connectionBezierOffset = (startPoint.y - endPoint.y) * connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.down * connectionBezierOffset, endPoint + Vector2.up * connectionBezierOffset,
                    Color.white, null, connectionBezierWidth);
            }
        }

        private SkillBranch GetSkillBranchAtPoint(Vector2 point, out bool draggable)
        {
            SkillBranch foundSkillBranch = null;
            draggable = false;
            foreach (SkillBranch skillBranch in selectedSkillTree.GetAllBranches())
            {
                if (skillBranch.GetRect().Contains(point))
                {
                    foundSkillBranch = skillBranch;
                }

                Rect draggingRect = new Rect(skillBranch.GetRect().position, skillBranch.GetDraggingRect().size);
                if (draggingRect.Contains(point))
                {
                    draggable = true;
                }
            }
            return foundSkillBranch;
        }
    }

}
