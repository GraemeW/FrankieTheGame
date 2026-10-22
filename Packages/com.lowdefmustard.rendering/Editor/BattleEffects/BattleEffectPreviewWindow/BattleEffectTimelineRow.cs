#if UNITY_EDITOR
namespace LowDefMustard.Rendering.Editor
{
    internal readonly struct BattleEffectTimelineRow
    {
        // Note:  one timeline row ~ an item (re-timed by any instances it sits in) or an instance's header (spanning its items)

        public string label { get; }
        public float startTime { get; }
        public float endTime { get; }
        public int depth { get; }
        public string instancePath { get; } // Headers only (null for items):  instance indices from the top recipe, e.g. "2/0"
        public bool isExpanded { get; }

        public bool isHeader => instancePath != null;

        public BattleEffectTimelineRow(string label, float startTime, float endTime, int depth, string instancePath, bool isExpanded)
        {
            this.label = label;
            this.startTime = startTime;
            this.endTime = endTime;
            this.depth = depth;
            this.instancePath = instancePath;
            this.isExpanded = isExpanded;
        }
    }
}
#endif
