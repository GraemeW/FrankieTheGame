using Frankie.Utils.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneNodeView : VisualElement
    {
        // Tunables
        private const float _headerHeight = 28f;
        private const float _borderWidth = 1f;
        private static readonly Color _headerColour = Color.gray1;
        private static readonly Color _bodyColour = Color.gray2;
        private static readonly Color _externalZoneBodyColour = Color.cornflowerBlue * 0.75f;

        // State
        private readonly ZoneNode zoneNode;
        private readonly ZoneGraphView zoneGraphView;
        private readonly Button linkButton;

        public ZoneNodeView(ZoneNode zoneNode, ZoneGraphView zoneGraphView)
        {
            this.zoneNode = zoneNode;
            this.zoneGraphView = zoneGraphView;
            if (zoneNode == null || zoneGraphView == null) { return; }
            
            InitializeNodeStyle();

            VisualElement nodeHeader = MakeNodeHeader();
            nodeHeader.AddManipulator(new StandardNodeDragManipulator(this, zoneNode, OnPositionChangedLive, OnPositionChangedComplete, () => zoneGraphView.zoomFactor));
            Add(nodeHeader);
            
            var idLabel = new Label($"--Unique ID: {zoneNode.GetNodeID()}--");
            nodeHeader.Add(idLabel);

            var headerSpacer = new VisualElement { style = { height = 10 } };
            Add(headerSpacer);
            
            var overrideIDField = new TextField("Override ID:")
            {
                value = zoneNode.GetNodeID(), 
                isDelayed = true,
                labelElement = { style = { minWidth = 80, width = 80} }
            };
            overrideIDField.RegisterValueChangedCallback(changeEvent =>
            {
                if (changeEvent.newValue == zoneNode.GetNodeID()) { return; }
                this.zoneGraphView.RequestNodeIDChange(zoneNode, changeEvent.newValue);
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

            if (!zoneGraphView.IsRootNode(zoneNode))
            {
                Button deleteButton = MakeAddRemoveButton(false);
                deleteButton.RegisterCallback<ClickEvent>(_ => this.zoneGraphView.RequestDelete(zoneNode));
                buttonRow.Add(deleteButton);
            }

            Button addButton = MakeAddRemoveButton(true);
            addButton.RegisterCallback<ClickEvent>(_ => this.zoneGraphView.RequestCreateChild(zoneNode));
            buttonRow.Add(addButton);
            
            var bottomSpacer = new VisualElement { style = { height = 5 } };
            Add(bottomSpacer);
        }

        public void RefreshLinkButton()
        {
            if (zoneNode == null) { return; }
            
            if (!zoneGraphView.isLinking)
            {
                linkButton.text = "link";
            }
            else if (zoneGraphView.GetLinkingParentNode() == zoneNode)
            {
                linkButton.text = "---";
            }
            else
            {
                linkButton.text = Zone.IsRelated(zoneGraphView.GetLinkingParentNode(), zoneNode) ? "unlink" : "child";
            }
        }

        public void ManualMoveZoneNode(Vector2 delta)
        {
            if (zoneNode == null) { return; }
            
            Vector2 newPosition = zoneNode.GetPosition() + delta;
            zoneNode.SetPosition(newPosition);
            style.left = newPosition.x;
            style.top = newPosition.y;
        }
        
        #region EventHandling
        private void OnPositionChangedLive()
        {
            zoneGraphView.NotifyNodeMoved();
            zoneGraphView.UpdateGroupsForNode(zoneNode);
        }

        private void OnPositionChangedComplete()
        {
            zoneGraphView.UpdateGroupsForNode(zoneNode, true);
        }

        private void OnLinkButtonClicked()
        {
            if (!zoneGraphView.isLinking)
            {
                zoneGraphView.BeginLinking(zoneNode);
            }
            else if (zoneGraphView.GetLinkingParentNode() == zoneNode)
            {
                zoneGraphView.CancelLinking();
            }
            else
            {
                zoneGraphView.CompleteLinking(zoneNode);
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
            style.backgroundColor = zoneNode.HasLinkedSceneReference() ? _externalZoneBodyColour : _bodyColour;
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
                    backgroundColor = _headerColour,
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
