using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    // Records every call received via plain public fields for test verification
    internal class TestZoneGraphView : IZoneGraphView
    {
        // Attributes / State
        public float zoomFactor { get; set; } = 1f;
        public bool isLinking { get; set; }
        public ZoneNode linkingParentNode;
        
        // Test State
        public bool isRootNodeResult;
        public readonly Dictionary<string, ZoneNodeView> nodeViewsByID = new();
        public ZoneNode requestNodeIDChangeNode;
        public string requestNodeIDChangeNewID;
        public ZoneNode requestDeleteNode;
        public ZoneNode requestCreateChildNode;
        public int notifyNodeMovedCallCount;
        public ZoneNode updateGroupsForNodeArg;
        public bool updateGroupsForNodeForceRecalcArg;
        public int updateGroupsForNodeCallCount;
        public ZoneNode beginLinkingNode;
        public int cancelLinkingCallCount;
        public ZoneNode completeLinkingNode;
        public ZoneNodeGroup requestDeleteGroupArg;
        public ZoneNodeGroup setGroupRectGroupArg;
        public Rect setGroupRectRectArg;

        // Getters
        public ZoneNode GetLinkingParentNode() => linkingParentNode;
        public bool IsRootNode(ZoneNode zoneNode) => isRootNodeResult;
        public bool TryGetZoneNodeView(string nodeID, out ZoneNodeView nodeView) => nodeViewsByID.TryGetValue(nodeID, out nodeView);

        // Pass-Through Methods
        public void RequestNodeIDChange(ZoneNode zoneNode, string newID)
        {
            requestNodeIDChangeNode = zoneNode;
            requestNodeIDChangeNewID = newID;
        }

        public void RequestDelete(ZoneNode nodeToDelete) => requestDeleteNode = nodeToDelete;
        public void RequestCreateChild(ZoneNode parentNode) => requestCreateChildNode = parentNode;
        public void NotifyNodeMoved() => notifyNodeMovedCallCount++;

        public void UpdateGroupsForNode(ZoneNode movedNode, bool forceRecalculation = false)
        {
            updateGroupsForNodeArg = movedNode;
            updateGroupsForNodeForceRecalcArg = forceRecalculation;
            updateGroupsForNodeCallCount++;
        }

        public void BeginLinking(ZoneNode parentNode) => beginLinkingNode = parentNode;
        public void CancelLinking() => cancelLinkingCallCount++;
        public void CompleteLinking(ZoneNode targetNode) => completeLinkingNode = targetNode;
        public void RequestDeleteGroup(ZoneNodeGroup group) => requestDeleteGroupArg = group;

        public void SetGroupRect(ZoneNodeGroup group, Rect rect)
        {
            setGroupRectGroupArg = group;
            setGroupRectRectArg = rect;
        }
    }
}
