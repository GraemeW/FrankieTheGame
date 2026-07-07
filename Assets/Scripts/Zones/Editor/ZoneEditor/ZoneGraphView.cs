using System.Collections.Generic;
using System.Linq;
using Frankie.Utils.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneGraphView : VisualElement
    {
        // State
        private Zone zone;
        private readonly ZoneEdgesLayer edgesLayer;
        private readonly VisualElement nodeLayer;
        private readonly StandardCanvasZoomManipulator zoomManipulator;
        private readonly Dictionary<string, ZoneNodeView> nodeViewLookup = new();
        private ZoneNode linkingParentNode;
        
        public ZoneGraphView()
        {
            VisualElement canvasContent = MakeCanvas();
            Add(canvasContent);

            var backgroundLayer = new ZoneBackgroundLayer();
            canvasContent.Add(backgroundLayer);

            edgesLayer = new ZoneEdgesLayer();
            canvasContent.Add(edgesLayer);

            nodeLayer = MakeNodesLayer();
            canvasContent.Add(nodeLayer);

            this.AddManipulator(new StandardCanvasPanManipulator(canvasContent));
            zoomManipulator = new StandardCanvasZoomManipulator(canvasContent);
            this.AddManipulator(zoomManipulator);
        }
        
        #region PublicGetters
        public bool IsRootNode(ZoneNode zoneNode) => zoneNode == zone.GetRootNode();
        public bool isLinking => linkingParentNode != null;
        public ZoneNode GetLinkingParentNode() => linkingParentNode;
        public float zoomFactor => zoomManipulator?.zoomFactor ?? 1.0f;
        #endregion
        
        #region PublicMethods
        public void SetZone(Zone newZone)
        {
            zone = newZone;
            linkingParentNode = null;
            RebuildNodes();
        }
        
        public void RequestNodeIDChange(ZoneNode zoneNode, string newID)
        {
            if (string.IsNullOrWhiteSpace(newID)) { return; }
            if (zone.GetNodeFromID(newID) != null) { return; } // ID already taken

            string oldID = zoneNode.GetNodeID();
            if (zoneNode.SetNodeID(newID))
            {
                zone.UpdateNodeID(oldID, newID);
            }
            RebuildNodes();
        }
        
        public void RequestCreateChild(ZoneNode parentNode)
        {
            zone.CreateChildNode(parentNode);
            RebuildNodes();
        }

        public void RequestDelete(ZoneNode nodeToDelete)
        {
            zone.DeleteNode(nodeToDelete);
            RebuildNodes();
        }
        
        public void NotifyNodeMoved() => RefreshEdges();

        public void BeginLinking(ZoneNode parentNode)
        {
            linkingParentNode = parentNode;
            RefreshAllLinkButtons();
        }

        public void CancelLinking()
        {
            linkingParentNode = null;
            RefreshAllLinkButtons();
        }

        public void CompleteLinking(ZoneNode targetNode)
        {
            if (linkingParentNode == null) { return; }
            zone.ToggleRelation(linkingParentNode, targetNode);
            linkingParentNode = null;
            RefreshAllLinkButtons();
            RefreshEdges();
        }
        #endregion

        #region PrivateMethods
        private void RebuildNodes()
        {
            nodeLayer.Clear();
            nodeViewLookup.Clear();

            if (zone == null) { return; }

            foreach (ZoneNode zoneNode in zone.GetAllNodes())
            {
                var nodeView = new ZoneNodeView(zoneNode, zone, this);
                nodeViewLookup[zoneNode.GetNodeID()] = nodeView;
                nodeLayer.Add(nodeView);
            }

            RefreshEdges();
        }

        private void RefreshEdges()
        {
            if (zone == null)
            {
                edgesLayer.SetEdges(null);
                return;
            }

            var edges = new List<(Rect from, Rect to)>();
            foreach (ZoneNode zoneNode in zone.GetAllNodes())
            {
                edges.AddRange(zone.GetAllChildren(zoneNode).Select(child => (zoneNode.GetRect(), child.GetRect())));
            }
            edgesLayer.SetEdges(edges);
        }

        private void RefreshAllLinkButtons()
        {
            foreach (ZoneNodeView nodeView in nodeViewLookup.Values)
            {
                nodeView.RefreshLinkButton();
            }
        }
        #endregion
        
        #region StaticUIBuilders
        private static VisualElement MakeCanvas()
        {
            return new VisualElement
            {
                name = "canvas-content",
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0
                }
            };
        }
        
        private static VisualElement MakeNodesLayer()
        {
            return new VisualElement
            {
                name = "zone-node-layer",
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0
                }
            };
        }
        #endregion
    }
}
