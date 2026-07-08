using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneGroupView : VisualElement
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

        public ZoneGroupView(ZoneNodeGroup zoneNodeGroup, ZoneGraphView zoneGraphView)
        {
            this.zoneNodeGroup = zoneNodeGroup;
            this.zoneGraphView = zoneGraphView;

            InitializeStyles();
            ApplyRectFromData();

            VisualElement header = MakeHeader();
            header.AddManipulator(new ZoneGroupDragManipulator(() => zoneNodeGroup.GetRect().position, OnHeaderDragged, () => zoneGraphView.zoomFactor));
            Add(header);
            
            TextField zoneNodeGroupNameField = MakeHeaderNameField(zoneNodeGroup.GetZoneNodeGroupName());
            zoneNodeGroupNameField.RegisterValueChangedCallback(changeEvent => { zoneNodeGroup.SetZoneNodeGroupName(changeEvent.newValue); });
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
        
        private void OnHeaderDragged(Vector2 newPosition)
        {
            Rect rect = zoneNodeGroup.GetRect();
            rect.position = newPosition;
            zoneGraphView.SetGroupRect(zoneNodeGroup, rect);
            ApplyRectFromData();
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

        private static TextField MakeHeaderNameField(string label)
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
            
            var textInput = headerNameField.Q<VisualElement>(className: TextField.inputUssClassName);
            if (textInput != null)
            {
                textInput.style.backgroundColor = Color.clear;
                textInput.style.borderTopColor = Color.clear;
                textInput.style.borderBottomColor = Color.clear;
                textInput.style.borderLeftColor = Color.clear;
                textInput.style.borderRightColor = Color.clear;
                textInput.style.borderTopWidth = 0;
                textInput.style.borderBottomWidth = 0;
                textInput.style.borderLeftWidth = 0;
                textInput.style.borderRightWidth = 0;
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
