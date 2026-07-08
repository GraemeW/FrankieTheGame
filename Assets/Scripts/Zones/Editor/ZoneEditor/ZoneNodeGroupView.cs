using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneNodeGroupView : VisualElement
    {
        // Tunables
        private const float _borderWidth = 2f;
        private const float _borderRadius = 12f;
        private const float _headerWidth = 200f;
        private const float _headerHeight = 25f;
        private const float _headerRadius = 6f;
        private const float _deleteButtonSize = 20f;
        private static readonly Color _groupColour = Color.lightPink * 0.65f;
        private static readonly Color _borderColour = Color.lightPink * 0.8f; 
        private static readonly Color _headerColour = new(0f, 0f, 0f, 0.2f);

        // State
        public ZoneNodeGroup zoneNodeGroup { get; }
        private readonly ZoneGraphView zoneGraphView;
        private readonly TextField zoneNodeGroupNameField;
        private readonly VisualElement zoneNodeGroupNameInput;

        public ZoneNodeGroupView(ZoneNodeGroup zoneNodeGroup, ZoneGraphView zoneGraphView)
        {
            this.zoneNodeGroup = zoneNodeGroup;
            this.zoneGraphView = zoneGraphView;
            if (zoneNodeGroup == null || zoneGraphView == null) { return; }

            InitializeStyles();
            ApplyRectFromData();

            VisualElement header = MakeHeader();
            header.AddManipulator(new ZoneNodeGroupDragManipulator(OnHeaderDragged, () => zoneGraphView.zoomFactor));
            header.RegisterCallback<MouseDownEvent>(OnHeaderMouseDown, TrickleDown.TrickleDown);
            Add(header);
            
            zoneNodeGroupNameField = MakeHeaderNameField(zoneNodeGroup.GetZoneNodeGroupName(), out zoneNodeGroupNameInput);
            zoneNodeGroupNameField.RegisterValueChangedCallback(changeEvent => { zoneNodeGroup.SetZoneNodeGroupName(changeEvent.newValue); });
            zoneNodeGroupNameField.RegisterCallback<FocusOutEvent>(_ => DisableNameFieldEditing());
            DisableNameFieldEditing();
            header.Add(zoneNodeGroupNameField);

            Button deleteButton = MakeDeleteButton();
            deleteButton.RegisterCallback<ClickEvent>(_ => zoneGraphView.RequestDeleteGroup(zoneNodeGroup));
            Add(deleteButton);
        }

        public void ApplyRectFromData()
        {
            Rect rect = zoneNodeGroup.GetRect();
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;
        }
        
        private void OnHeaderDragged(Vector2 delta)
        {
            Rect newPosition = zoneNodeGroup.GetRect();
            newPosition.position += delta;
            zoneGraphView.SetGroupRect(zoneNodeGroup, newPosition);
            foreach (string nodeID in zoneNodeGroup.GetContainedNodeIDs())
            {
                if (!zoneGraphView.TryGetZoneNodeView(nodeID, out ZoneNodeView zoneNodeView)) { continue; }
                zoneNodeView.ManualMoveZoneNode(delta);
            }
            zoneGraphView.NotifyNodeMoved();
            ApplyRectFromData();
        }

        private void OnHeaderMouseDown(MouseDownEvent mouseDownEvent)
        {
            if (mouseDownEvent.clickCount != 2) { return; }

            EnableNameFieldEditing();
            mouseDownEvent.StopPropagation();
        }

        private void EnableNameFieldEditing()
        {
            zoneNodeGroupNameField.SetEnabled(true);
            zoneNodeGroupNameField.Focus();
            zoneNodeGroupNameField.SelectAll();
        }

        private void DisableNameFieldEditing()
        {
            zoneNodeGroupNameField.SetEnabled(false);
        }
        
        private void InitializeStyles()
        {
            name = "zone-group-view";
            style.position = Position.Absolute;
            pickingMode = PickingMode.Ignore;
            
            style.backgroundColor = _groupColour;
            style.borderTopWidth = _borderWidth;
            style.borderBottomWidth = _borderWidth;
            style.borderLeftWidth = _borderWidth;
            style.borderRightWidth = _borderWidth;
            style.borderTopColor = _borderColour;
            style.borderBottomColor = _borderColour;
            style.borderLeftColor = _borderColour;
            style.borderRightColor = _borderColour;
            style.borderTopLeftRadius = _borderRadius;
            style.borderTopRightRadius = _borderRadius;
            style.borderBottomLeftRadius = _borderRadius;
            style.borderBottomRightRadius = _borderRadius;
        }
        
        private static VisualElement MakeHeader()
        {
            return new VisualElement
            {
                name = "zone-group-header",
                style =
                {
                    position = Position.Absolute,
                    left = 5,
                    top = 5,
                    width = _headerWidth,
                    height = _headerHeight,
                    alignItems =  Align.Center,
                    justifyContent = Justify.Center,
                    backgroundColor = _headerColour,
                    color = _borderColour,
                    borderTopColor = _headerColour,
                    borderBottomColor = _headerColour,
                    borderLeftColor = _headerColour,
                    borderRightColor = _headerColour,
                    borderTopLeftRadius = _headerRadius,
                    borderTopRightRadius = _headerRadius,
                    borderBottomLeftRadius = _headerRadius,
                    borderBottomRightRadius = _headerRadius
                }
            };
        }

        private static TextField MakeHeaderNameField(string label, out VisualElement zoneNodeGroupNameInput)
        {
            var headerNameField = new TextField
            {
                value = label,
                style =
                {
                    justifyContent = Justify.Center, 
                    alignContent = Align.Center,
                    backgroundColor = Color.clear,
                    borderTopColor = Color.clear,
                    borderBottomColor = Color.clear,
                    borderLeftColor = Color.clear,
                    borderRightColor = Color.clear,
                }
            };

            zoneNodeGroupNameInput = headerNameField.Q<VisualElement>(className: TextField.inputUssClassName);
            if (zoneNodeGroupNameInput != null)
            {
                zoneNodeGroupNameInput.style.backgroundColor = Color.clear;
                zoneNodeGroupNameInput.style.borderTopColor = Color.clear;
                zoneNodeGroupNameInput.style.borderBottomColor = Color.clear;
                zoneNodeGroupNameInput.style.borderLeftColor = Color.clear;
                zoneNodeGroupNameInput.style.borderRightColor = Color.clear;
                zoneNodeGroupNameInput.style.borderTopWidth = 0;
                zoneNodeGroupNameInput.style.borderBottomWidth = 0;
                zoneNodeGroupNameInput.style.borderLeftWidth = 0;
                zoneNodeGroupNameInput.style.borderRightWidth = 0;
            }
            return headerNameField;
        }

        private static Button MakeDeleteButton()
        {
            return new Button
            {
                text = "x",
                style =
                {
                    position = Position.Absolute,
                    top = 5,
                    right = 5,
                    width = _deleteButtonSize,
                    height = _deleteButtonSize,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    backgroundColor = _headerColour,
                    color = _borderColour,
                    borderTopColor = _headerColour,
                    borderBottomColor = _headerColour,
                    borderLeftColor = _headerColour,
                    borderRightColor = _headerColour
                }
            };
        }
    }
}
