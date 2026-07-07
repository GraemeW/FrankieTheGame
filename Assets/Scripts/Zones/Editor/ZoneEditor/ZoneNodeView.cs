using Frankie.Utils.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneNodeView : VisualElement
    {
        // Tunables
        private const float _headerHeight = 28f;
        private const float _nodePaddingHorizontal = 20f;
        private const float _nodePaddingVertical = 16f;
        private const float _borderWidth = 1f;
        private const float _bodyPadding = 6f;
        private static readonly Color _headerColor = Color.gray1;
        private static readonly Color _bodyColor = Color.gray2;

        // State
        private readonly ZoneNode zoneNode;
        private readonly ZoneGraphView graphView;
        private readonly Button linkButton;

        public ZoneNodeView(ZoneNode zoneNode, Zone _, ZoneGraphView graphView)
        {
            this.zoneNode = zoneNode;
            this.graphView = graphView;
            
            InitializeNodeStyle();

            VisualElement nodeHeader = MakeNodeHeader();
            nodeHeader.AddManipulator(new StandardNodeDragManipulator(this, zoneNode, OnPositionChanged, () => graphView.zoomFactor));
            Add(nodeHeader);
            
            var idLabel = new Label($"--Unique ID: {zoneNode.GetNodeID()}--");
            nodeHeader.Add(idLabel);

            var headerSpacer = new VisualElement { style = { height = 10 } };
            Add(headerSpacer);
            
            var overrideIDField = new TextField("Override ID:") { value = zoneNode.GetNodeID(), isDelayed = true };
            overrideIDField.RegisterValueChangedCallback(changeEvent =>
            {
                if (changeEvent.newValue == zoneNode.GetNodeID()) { return; }
                this.graphView.RequestNodeIDChange(zoneNode, changeEvent.newValue);
            });
            Add(overrideIDField);

            var cardSpacer = new VisualElement { style = { flexGrow = 1 } };
            Add(cardSpacer);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            Add(buttonRow);

            linkButton = MakeLinkButton();
            linkButton.RegisterCallback<ClickEvent>(_ => OnLinkButtonClicked());
            buttonRow.Add(linkButton);
            RefreshLinkButton();
            
            var buttonSpacer = new VisualElement { style = { flexGrow = 1 } };
            buttonRow.Add(buttonSpacer);

            if (!graphView.IsRootNode(zoneNode))
            {
                Button deleteButton = MakeAddRemoveButton(false);
                deleteButton.RegisterCallback<ClickEvent>(_ => this.graphView.RequestDelete(zoneNode));
                buttonRow.Add(deleteButton);
            }

            Button addButton = MakeAddRemoveButton(true);
            addButton.RegisterCallback<ClickEvent>(_ => this.graphView.RequestCreateChild(zoneNode));
            buttonRow.Add(addButton);
        }

        public void RefreshLinkButton()
        {
            if (!graphView.isLinking)
            {
                linkButton.text = "link";
            }
            else if (graphView.GetLinkingParentNode() == zoneNode)
            {
                linkButton.text = "---";
            }
            else
            {
                linkButton.text = Zone.IsRelated(graphView.GetLinkingParentNode(), zoneNode) ? "unlink" : "child";
            }
        }
        
        #region EventHandling
        private void OnPositionChanged()
        {
            graphView.NotifyNodeMoved();
        }

        private void OnLinkButtonClicked()
        {
            if (!graphView.isLinking)
            {
                graphView.BeginLinking(zoneNode);
            }
            else if (graphView.GetLinkingParentNode() == zoneNode)
            {
                graphView.CancelLinking();
            }
            else
            {
                graphView.CompleteLinking(zoneNode);
            }
        }
        #endregion
        
        #region PrivateMethods
        private void InitializeNodeStyle()
        {
            if (zoneNode == null) { return; }
            Rect rect = zoneNode.GetRect();
            style.position = Position.Absolute;
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;
            style.backgroundColor = _bodyColor;
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = _borderWidth;
            style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = Color.black;
            style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        }
        
        private static VisualElement MakeNodeHeader()
        {
            return new VisualElement
            {
                name = "drag-header",
                style =
                {
                    height = _headerHeight,
                    backgroundColor = _headerColor,
                    justifyContent = Justify.Center
                }
            };
        }
        
        private static Button MakeLinkButton()
        {
            return new Button()
            {
                text = "link",
                style =
                {
                    width = 100
                }
            };
        }

        private static Button MakeAddRemoveButton(bool add)
        {
            return new Button()
            {
                text = add ? "+" : "-",
                style =
                {
                    width = 50
                }
            };
        }
        #endregion
    }
}
