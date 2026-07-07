using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneGroupView : VisualElement
    {
        // Tunables
        private const float _headerWidth = 36f;
        private const float _headerHeight = 18f;
        private const float _deleteButtonSize = 16f;
        private static readonly Color _groupColor = Color.lightPink * 0.65f;
        private static readonly Color _headerColor = new(0f, 0f, 0f, 0.2f);

        // State
        public ZoneNodeGroup zoneNodeGroup { get; }
        private readonly ZoneGraphView zoneGraphView;

        public ZoneGroupView(ZoneNodeGroup zoneNodeGroup, ZoneGraphView zoneGraphView)
        {
            this.zoneNodeGroup = zoneNodeGroup;
            this.zoneGraphView = zoneGraphView;

            name = "zone-group-view";
            style.position = Position.Absolute;
            style.backgroundColor = _groupColor;
            pickingMode = PickingMode.Ignore;
            ApplyRectFromData();

            VisualElement header = MakeHeader();
            Add(header);
            header.AddManipulator(new ZoneGroupDragManipulator(() => zoneNodeGroup.GetRect().position, OnHeaderDragged, () => zoneGraphView.zoomFactor));

            Button deleteButton = MakeDeleteButton();
            deleteButton.RegisterCallback<ClickEvent>(_ => zoneGraphView.RequestDeleteGroup(zoneNodeGroup));
            Add(deleteButton);
        }

        private void OnHeaderDragged(Vector2 newPosition)
        {
            Rect rect = zoneNodeGroup.GetRect();
            rect.position = newPosition;
            zoneGraphView.SetGroupRect(zoneNodeGroup, rect);
            ApplyRectFromData();
        }

        public void ApplyRectFromData()
        {
            Rect rect = zoneNodeGroup.GetRect();
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;
        }

        private static VisualElement MakeHeader()
        {
            return new VisualElement
            {
                name = "zone-group-header",
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    width = _headerWidth,
                    height = _headerHeight,
                    backgroundColor = _headerColor
                }
            };
        }

        private static Button MakeDeleteButton()
        {
            return new Button
            {
                text = "x",
                style =
                {
                    position = Position.Absolute,
                    top = 0,
                    right = 0,
                    width = _deleteButtonSize,
                    height = _deleteButtonSize,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0
                }
            };
        }
    }
}
