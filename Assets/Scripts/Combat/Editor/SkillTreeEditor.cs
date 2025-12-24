using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Frankie.Combat.UIEditor
{
    public class SkillTreeEditor : EditorWindow
    {
        // Tunables
        private SkillTree selectedSkillTree;
        private const int _branchPadding = 20;
        private const int _branchBorder = 12;
        private const float _addRemoveButtonMultiplier = 0.1f;
        private const float _connectionBezierOffsetMultiplier = 0.7f;
        private const float _connectionBezierWidth = 2f;
        private const string _backgroundName = "background";
        private const float _backgroundSize = 50f;

        // State
        [NonSerialized] private GUIStyle skillBranchStyle;
        [NonSerialized] private GUIStyle rootSkillBranchStyle;
        [NonSerialized] private SkillBranch selectedSkillBranch;
        [NonSerialized] private bool draggable = false;
        [NonSerialized] private Vector2 draggingOffset;
        [NonSerialized] private SkillBranch creatingSkillBranch;
        [NonSerialized] private SkillBranchMapping creatingSkillBranchPosition;
        [NonSerialized] private SkillBranch deletingSkillBranch;
        [NonSerialized] private SkillBranchMapping deletingSkillBranchMapping;
        [NonSerialized] private Vector2 scrollPosition;

        [NonSerialized] private float scrollMinX = 1;
        [NonSerialized] private float scrollMinY = 1;
        [NonSerialized] private float scrollMaxX = 1;
        [NonSerialized] private float scrollMaxY = 1;

        [MenuItem("Window/SkillTree Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(SkillTreeEditor), false, "SkillTree Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var skillTree = EditorUtility.EntityIdToObject(instanceID) as SkillTree;
            if (skillTree == null) return false;
            skillTree.CreateRootSkillBranchIfMissing();
            ShowEditorWindow();
            return true;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            SetupSkillBranchStyle();
        }

        private void SetupSkillBranchStyle()
        {
            skillBranchStyle = new GUIStyle
            {
                normal = { background = EditorGUIUtility.Load("node0") as Texture2D },
                padding = new RectOffset(_branchPadding, _branchPadding / 2, _branchPadding / 2, _branchPadding),
                border = new RectOffset(_branchBorder, _branchBorder, _branchBorder, _branchBorder)
            };

            rootSkillBranchStyle = new GUIStyle
            {
                normal = { background = EditorGUIUtility.Load("node1") as Texture2D },
                padding = new RectOffset(_branchPadding, _branchPadding / 2, _branchPadding / 2, _branchPadding),
                border = new RectOffset(_branchBorder, _branchBorder, _branchBorder, _branchBorder)
            };
        }

        private void OnSelectionChanged()
        {
            var newSkillTree = Selection.activeObject as SkillTree;
            if (newSkillTree == null) return;
            selectedSkillTree = newSkillTree;
            Repaint();
        }

        private void OnGUI()
        {
            if (selectedSkillTree == null) { EditorGUILayout.LabelField("No tree selected."); }
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
                    selectedSkillTree.CreateChildSkillBranch(creatingSkillBranch, creatingSkillBranchPosition);
                    creatingSkillBranch = null;
                    creatingSkillBranchPosition = default;
                }
            }
        }

        private void DrawBackground()
        {
            Rect canvas = GUILayoutUtility.GetRect(scrollMaxX, scrollMaxY);
            var backgroundTexture = Resources.Load(_backgroundName) as Texture2D;
            if (backgroundTexture == null) { return; }
            GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, new Rect(0, 0, canvas.width / _backgroundSize, canvas.height / _backgroundSize));
            ResetOriginForNegativeBranchPositions();

            // Reset scrolling limits, to be updated after draw nodes
            scrollMaxX = 1f;
            scrollMaxX = 1f;
            scrollMinX = 1f;
            scrollMinY = 1f;
        }

        private void ProcessEvents()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
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

                    break;
                }
                case EventType.MouseDrag when selectedSkillBranch != null:
                {
                    if (draggable)
                    {
                        Vector2 currentMousePosition = Event.current.mousePosition;
                        selectedSkillBranch.SetPosition(currentMousePosition + draggingOffset);
                        GUI.changed = true;
                    }

                    break;
                }
                case EventType.MouseDrag when selectedSkillBranch == null:
                    scrollPosition = draggingOffset - Event.current.mousePosition;
                    GUI.changed = true;
                    break;
                case EventType.MouseUp:
                    selectedSkillBranch = null;
                    draggingOffset = new Vector2();
                    break;
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
                if (!(scrollMinX < 0) && !(scrollMinY < 0)) continue;
                Vector2 currentPosition = skillBranch.GetPosition();
                Vector2 shiftedPosition = new Vector2(currentPosition.x - scrollMinX, currentPosition.y - scrollMinY);
                skillBranch.SetPosition(shiftedPosition);
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
            if (skillBranch == selectedSkillTree.GetRootSkillBranch()) return;
            if (!GUILayout.Button("-", GUILayout.Width(skillBranch.GetRect().width * _addRemoveButtonMultiplier))) return;
            deletingSkillBranch = skillBranch;
            deletingSkillBranchMapping = skillBranch.GetParentBranchMapping();
        }

        private void DrawBranchAddButtons(SkillBranch skillBranch, SkillBranchMapping skillBranchMapping)
        {
            // Set tags to create/delete at end of OnGUI to avoid operating on list while iterating over it
            if (skillBranch.HasBranch(skillBranchMapping) || !skillBranch.HasSkill(skillBranchMapping)) return; // only allow branch if skill populated
            if (!GUILayout.Button("+", GUILayout.Width(skillBranch.GetRect().width * _addRemoveButtonMultiplier))) return;
            creatingSkillBranch = skillBranch;
            creatingSkillBranchPosition = skillBranchMapping;
        }

        private void DrawConnections(SkillBranch skillBranch)
        {
            SkillBranch upSkillBranch = selectedSkillTree.GetChildSkillBranch(skillBranch, SkillBranchMapping.up);
            SkillBranch leftSkillBranch = selectedSkillTree.GetChildSkillBranch(skillBranch, SkillBranchMapping.left);
            SkillBranch rightSkillBranch = selectedSkillTree.GetChildSkillBranch(skillBranch, SkillBranchMapping.right);
            SkillBranch downSkillBranch = selectedSkillTree.GetChildSkillBranch(skillBranch, SkillBranchMapping.down);

            if (upSkillBranch != null)
            {
                var startPoint = new Vector2(skillBranch.GetRect().center.x, skillBranch.GetRect().yMin);
                var endPoint = new Vector2(upSkillBranch.GetRect().center.x, upSkillBranch.GetRect().yMax);
                float connectionBezierOffset = (endPoint.y - startPoint.y) * _connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.up * connectionBezierOffset, endPoint + Vector2.down * connectionBezierOffset,
                    Color.white, null, _connectionBezierWidth);
            }
            if (leftSkillBranch != null)
            {
                var startPoint = new Vector2(skillBranch.GetRect().xMin, skillBranch.GetRect().center.y);
                var endPoint = new Vector2(leftSkillBranch.GetRect().xMax, leftSkillBranch.GetRect().center.y);
                float connectionBezierOffset = (startPoint.x - endPoint.x) * _connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.left * connectionBezierOffset, endPoint + Vector2.right * connectionBezierOffset,
                    Color.white, null, _connectionBezierWidth);
            }
            if (rightSkillBranch != null)
            {
                var startPoint = new Vector2(skillBranch.GetRect().xMax, skillBranch.GetRect().center.y);
                var endPoint = new Vector2(rightSkillBranch.GetRect().xMin, rightSkillBranch.GetRect().center.y);
                float connectionBezierOffset = (endPoint.x - startPoint.x) * _connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.right * connectionBezierOffset, endPoint + Vector2.left * connectionBezierOffset,
                    Color.white, null, _connectionBezierWidth);
            }
            if (downSkillBranch != null)
            {
                var startPoint = new Vector2(skillBranch.GetRect().center.x, skillBranch.GetRect().yMax);
                var endPoint = new Vector2(downSkillBranch.GetRect().center.x, downSkillBranch.GetRect().yMin);
                float connectionBezierOffset = (startPoint.y - endPoint.y) * _connectionBezierOffsetMultiplier;
                Handles.DrawBezier(startPoint, endPoint,
                    startPoint + Vector2.down * connectionBezierOffset, endPoint + Vector2.up * connectionBezierOffset,
                    Color.white, null, _connectionBezierWidth);
            }
        }

        private SkillBranch GetSkillBranchAtPoint(Vector2 point, out bool getDraggable)
        {
            SkillBranch foundSkillBranch = null;
            getDraggable = false;
            foreach (SkillBranch skillBranch in selectedSkillTree.GetAllBranches())
            {
                if (skillBranch.GetRect().Contains(point))
                {
                    foundSkillBranch = skillBranch;
                }

                Rect draggingRect = new Rect(skillBranch.GetRect().position, skillBranch.GetDraggingRect().size);
                if (draggingRect.Contains(point))
                {
                    getDraggable = true;
                }
            }
            return foundSkillBranch;
        }
    }

}
