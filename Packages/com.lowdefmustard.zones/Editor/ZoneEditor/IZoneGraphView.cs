using UnityEngine;

namespace LowDefMustard.Zones.Editor
{
    // ZoneGraphView implements as ZoneNodeView / ZoneNodeGroupView test seam
    public interface IZoneGraphView
    {
        float zoomFactor { get; }
        bool isLinking { get; }
        ZoneNode GetLinkingParentNode();
        bool IsRootNode(ZoneNode zoneNode);
        void RequestNodeIDChange(ZoneNode zoneNode, string newID);
        void RequestDelete(ZoneNode nodeToDelete);
        void RequestCreateChild(ZoneNode parentNode);
        void NotifyNodeMoved();
        void UpdateGroupsForNode(ZoneNode movedNode, bool forceRecalculation = false);
        void BeginLinking(ZoneNode parentNode);
        void CancelLinking();
        void CompleteLinking(ZoneNode targetNode);
        void RequestDeleteGroup(ZoneNodeGroup group);
        void SetGroupRect(ZoneNodeGroup group, Rect rect);
        bool TryGetZoneNodeView(string nodeID, out ZoneNodeView nodeView);
    }
}
