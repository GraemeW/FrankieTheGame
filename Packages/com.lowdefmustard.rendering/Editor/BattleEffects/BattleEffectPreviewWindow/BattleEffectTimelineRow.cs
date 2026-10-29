#if UNITY_EDITOR
namespace LowDefMustard.Rendering.Editor
{
    internal readonly struct BattleEffectTimelineRow
    {
        public string label { get; }
        public float startTime { get; }
        public float endTime { get; }
        public int depth { get; }
        public string instancePath { get; } // Headers only (null for items):  instance indices from the top recipe, e.g. "2/0"
        public bool isExpanded { get; }
        public bool isHidden { get; } // Preview-hidden itself or under a hidden instance (see BattleEffectPreviewVisibility)

        public bool isHeader => instancePath != null;

        public BattleEffectTimelineRow(string label, float startTime, float endTime, int depth, string instancePath, bool isExpanded, bool isHidden)
        {
            this.label = label;
            this.startTime = startTime;
            this.endTime = endTime;
            this.depth = depth;
            this.instancePath = instancePath;
            this.isExpanded = isExpanded;
            this.isHidden = isHidden;
        }
    }
}
#endif
