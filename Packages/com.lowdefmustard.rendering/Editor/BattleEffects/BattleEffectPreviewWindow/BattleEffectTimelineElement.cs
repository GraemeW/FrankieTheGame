#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    public sealed class BattleEffectTimelineElement : VisualElement
    {
        // Structure / Behaviour Overview:
        //  - One bar per recipe item over its time window w/ a playhead
        //      - Clicking / dragging scrubs through the playback
        //  - Each instance adds a header row (its bar spans its items), collapsed by default
        //  - Rows (items + instance headers) are sorted by start time, then end time, at each level;  an expanded instance's rows stay under its header
        //      - Clicking a header's label expands / collapses it:  its items (re-timed by the instance's offset) + nested instances, indented
        //  - Rows are only rebuilt when the rows' layout changes (labels / timings / expanded instances)
        //      - Otherwise just the playhead + highlights move
        // Note:  Internal methods/properties are for test visibility

        // Const Tunables
        private const float _rowHeight = 16f;
        internal const float labelWidth = 110f;
        private const float _barInset = 2f;
        private const float _playheadWidth = 2f;
        private const float _indentWidth = 10f;
        private static readonly Color _barColour = new(0.35f, 0.55f, 0.85f, 1f);
        private static readonly Color _activeBarColour = new(0.55f, 0.8f, 1f, 1f);
        private static readonly Color _headerBarColour = new(0.55f, 0.4f, 0.75f, 1f);
        private static readonly Color _activeHeaderBarColour = new(0.8f, 0.6f, 1f, 1f);
        private static readonly Color _playheadColour = new(1f, 0.4f, 0.3f, 1f);
        private static readonly Color _trackColour = new(0f, 0f, 0f, 0.25f);

        // State
        private BattleEffectRecipe recipe;
        private float totalTime = 1f;
        private float time;
        private bool isScrubbing = false;
        private int layoutHash;
        private VisualElement playhead;
        private readonly List<BattleEffectTimelineRow> rows = new();
        private readonly List<VisualElement> bars = new();
        private readonly HashSet<string> expandedInstances = new();

        // Events
        public event System.Action<float> scrubbed;

        // Constructor
        public BattleEffectTimelineElement()
        {
            style.marginTop = 6f;
            RegisterCallback<PointerDownEvent>(HandlePointerDown);
            RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            RegisterCallback<PointerUpEvent>(HandlePointerUp);
        }

        #region PublicMethods
        public void Refresh(BattleEffectRecipe setRecipe, float setTime, float setTotalTime)
        {
            if (setRecipe != recipe) { expandedInstances.Clear(); }
            recipe = setRecipe;
            totalTime = Mathf.Max(setTotalTime, 0.01f);
            time = setTime;
            CollectRows();
            int newLayoutHash = GetLayoutHash();
            if (newLayoutHash != layoutHash || playhead == null)
            {
                layoutHash = newLayoutHash;
                Rebuild();
            }
            UpdateTime();
        }
        #endregion
        
        #region EventHandlers
        private void HandlePointerDown(PointerDownEvent pointerDownEvent)
        {
            // A header's label toggles its instance (no scrub)
            if (pointerDownEvent.localPosition.x < labelWidth && TryToggleRow(Mathf.FloorToInt(pointerDownEvent.localPosition.y / _rowHeight))) { return; }
            isScrubbing = true;
            this.CapturePointer(pointerDownEvent.pointerId);
            Scrub(pointerDownEvent.localPosition);
        }

        private void HandlePointerMove(PointerMoveEvent pointerMoveEvent)
        {
            if (isScrubbing) { Scrub(pointerMoveEvent.localPosition); }
        }

        private void HandlePointerUp(PointerUpEvent pointerUpEvent)
        {
            isScrubbing = false;
            this.ReleasePointer(pointerUpEvent.pointerId);
        }
        #endregion

        #region PrivateMethods
        internal bool TryToggleRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= rows.Count || !rows[rowIndex].isHeader) { return false; }
            string instancePath = rows[rowIndex].instancePath;
            if (!expandedInstances.Remove(instancePath)) { expandedInstances.Add(instancePath); }
            Refresh(recipe, time, totalTime);
            return true;
        }

        private void CollectRows()
        {
            rows.Clear();
            if (recipe != null) { rows.AddRange(CollectRows(recipe, 0f, 0, "")); }
        }

        private List<BattleEffectTimelineRow> CollectRows(BattleEffectRecipe rowRecipe, float offset, int depth, string pathPrefix)
        {
            // The recipe's items + a header per instance (w/ its rows below it when expanded)
            //  - sorted by start time -> then (earlier) end time -> then items/instances by recipe order
            
            var blocks = new List<(float startTime, float endTime, List<BattleEffectTimelineRow> blockRows)>();
            int index = 0;
            foreach (BattleEffectItem item in rowRecipe.GetItems())
            {
                if (item == null) { continue; }
                string label = string.IsNullOrEmpty(item.GetLabel()) ? $"Item {index}" : item.GetLabel();
                var itemRow = new BattleEffectTimelineRow(label, offset + item.GetStartTime(), offset + item.GetEndTime(), depth, null, false);
                blocks.Add((itemRow.startTime, itemRow.endTime, new List<BattleEffectTimelineRow> { itemRow }));
                index++;
            }

            if (depth < BattleEffectRecipe.maxInstanceDepth)
            {
                IReadOnlyList<BattleEffectInstance> instances = rowRecipe.GetInstances();
                for (int i = 0; i < instances.Count; i++)
                {
                    BattleEffectRecipe instanced = instances[i]?.GetRecipe();
                    if (instanced == null) { continue; }
                    string instancePath = pathPrefix + i;
                    float instanceOffset = offset + instances[i].GetStartOffset();
                    bool isExpanded = expandedInstances.Contains(instancePath);
                    string label = string.IsNullOrEmpty(instances[i].GetLabel()) ? instanced.name : instances[i].GetLabel();
                    var headerRow = new BattleEffectTimelineRow(label, instanceOffset + GetStartTime(instanced, depth + 1), instanceOffset + instanced.GetTotalEndTime(), depth, instancePath, isExpanded);
                    var blockRows = new List<BattleEffectTimelineRow> { headerRow };
                    if (isExpanded) { blockRows.AddRange(CollectRows(instanced, instanceOffset, depth + 1, instancePath + "/")); }
                    blocks.Add((headerRow.startTime, headerRow.endTime, blockRows));
                }
            }

            var sortedRows = new List<BattleEffectTimelineRow>();
            foreach ((float _, float _, List<BattleEffectTimelineRow> blockRows) in blocks.OrderBy(block => block.startTime).ThenBy(block => block.endTime)) { sortedRows.AddRange(blockRows); }
            return sortedRows;
        }

        private static float GetStartTime(BattleEffectRecipe startRecipe, int depth)
        {
            // Earliest item start, incl. nested instances (0 if there are none)
            float startTime = float.MaxValue;
            foreach (BattleEffectItem item in startRecipe.GetItems())
            {
                if (item != null) { startTime = Mathf.Min(startTime, item.GetStartTime()); }
            }
            
            if (depth < BattleEffectRecipe.maxInstanceDepth)
            {
                foreach (BattleEffectInstance instance in startRecipe.GetInstances())
                {
                    if (instance?.GetRecipe() != null) { startTime = Mathf.Min(startTime, instance.GetStartOffset() + GetStartTime(instance.GetRecipe(), depth + 1)); }
                }
            }
            return Mathf.Approximately(startTime, float.MaxValue) ? 0f : startTime;
        }

        private void Rebuild()
        {
            Clear();
            bars.Clear();
            playhead = null;
            if (recipe == null) { return; }

            foreach (BattleEffectTimelineRow row in rows) { Add(BuildRow(row)); }
            style.height = rows.Count * _rowHeight;

            playhead = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = 0f,
                    bottom = 0f,
                    width = _playheadWidth,
                    left = new Length(0f, LengthUnit.Pixel),
                    backgroundColor = _playheadColour
                }
            };
            playhead.RegisterCallback<GeometryChangedEvent>(_ => UpdateTime());
            Add(playhead);
        }

        private void UpdateTime()
        {
            if (playhead == null) { return; }
            float trackWidth = Mathf.Max(contentRect.width - labelWidth, 0f);
            playhead.style.left = labelWidth + Mathf.Clamp01(time / totalTime) * trackWidth;
            for (int i = 0; i < bars.Count; i++)
            {
                BattleEffectTimelineRow row = rows[i];
                bool isActive = time >= row.startTime && time <= row.endTime;
                bars[i].style.backgroundColor = row.isHeader ? (isActive ? _activeHeaderBarColour : _headerBarColour) : (isActive ? _activeBarColour : _barColour);
            }
        }

        private VisualElement BuildRow(BattleEffectTimelineRow timelineRow)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexDirection = FlexDirection.Row, height = _rowHeight } };
            string label = timelineRow.isHeader ? $"{(timelineRow.isExpanded ? "-" : "+")} {timelineRow.label}" : timelineRow.label;
            row.Add(new Label(label)
            {
                tooltip = timelineRow.isHeader ? $"{timelineRow.label} (instance:  click to {(timelineRow.isExpanded ? "collapse" : "expand")})" : timelineRow.label,
                style =
                {
                    width = labelWidth,
                    flexShrink = 0f,
                    paddingLeft = timelineRow.depth * _indentWidth,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    unityFontStyleAndWeight = timelineRow.isHeader ? FontStyle.Bold : FontStyle.Normal,
                    overflow = Overflow.Hidden,
                    whiteSpace = WhiteSpace.NoWrap,
                    textOverflow = TextOverflow.Ellipsis
                }
            });

            var track = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexGrow = 1f, backgroundColor = _trackColour } };
            var bar = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = _barInset,
                    bottom = _barInset,
                    left = new Length(Mathf.Clamp01(timelineRow.startTime / totalTime) * 100f, LengthUnit.Percent),
                    width = new Length(Mathf.Clamp01((timelineRow.endTime - timelineRow.startTime) / totalTime) * 100f, LengthUnit.Percent)
                }
            };
            bars.Add(bar);
            track.Add(bar);
            row.Add(track);
            return row;
        }

        private void Scrub(Vector2 localPosition)
        {
            if (TryGetScrubTime(localPosition.x, contentRect.width, totalTime, out float scrubTime)) { scrubbed?.Invoke(scrubTime); }
        }

        internal static bool TryGetScrubTime(float localX, float width, float totalTime, out float scrubTime)
        {
            scrubTime = 0f;
            float trackWidth = width - labelWidth;
            if (trackWidth <= 0f) { return false; }
            scrubTime = Mathf.Clamp01((localX - labelWidth) / trackWidth) * totalTime;
            return true;
        }
        
        private int GetLayoutHash()
        {
            if (recipe == null) { return 0; }
            int hash = recipe.GetHashCode() ^ totalTime.GetHashCode();
            foreach (BattleEffectTimelineRow row in rows)
            {
                hash = hash * 31 + (row.label?.GetHashCode() ?? 0);
                hash = hash * 31 + row.startTime.GetHashCode();
                hash = hash * 31 + row.endTime.GetHashCode();
                hash = hash * 31 + (row.instancePath?.GetHashCode() ?? 0) + (row.isExpanded ? 1 : 0);
            }
            return hash;
        }
        #endregion
    }
}
#endif
