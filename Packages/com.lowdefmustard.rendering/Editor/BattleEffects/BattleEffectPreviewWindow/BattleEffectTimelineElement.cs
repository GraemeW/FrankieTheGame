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
        //  - An overall row sits on top:  one bar per stretch where any shown item plays (instances flattened) ~ its label collapses the timeline to just that row
        //  - Each instance adds a header row (its bar spans its items) collapsed by default ~ when expanded, its rows stay under its header
        //  - Rows (items + instance headers) are sorted by a-start time->b-end time && greyed if hidden
        // Note:  Internal methods/properties are for test visibility

        // Const Tunables
        private const float _rowHeight = 16f;
        internal const float labelWidth = 110f;
        private const float _barInset = 2f;
        private const float _playheadWidth = 2f;
        private const float _indentWidth = 10f;
        private const string _overallLabel = "Overall";
        private static readonly Color _playheadColour = new(1f, 0.4f, 0.3f, 1f);
        private static readonly Color _trackColour = new(0f, 0f, 0f, 0.25f);

        // State
        private BattleEffectRecipe recipe;
        
        private float totalTime = 1f;
        private float time;
        private bool isScrubbing = false;
        private bool isCollapsed = false;
        
        private int layoutHash;
        private readonly HashSet<string> expandedInstances = new();
        
        private bool areRowsStale = true;
        private int collectedRevision = -1;
        private int collectedInstanceRevision = -1;
        
        // Element References
        private VisualElement playhead;
        private readonly List<Vector2> overallSpans = new(); // (start, end) seconds, merged
        private readonly List<VisualElement> overallBars = new();
        private readonly List<BattleEffectTimelineRow> rows = new();
        private readonly List<VisualElement> bars = new();

        // Events
        public event System.Action<float> scrubbed;
        public event System.Action scrubEnded;
        public event System.Action<bool> collapseToggled;

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
            if (setRecipe != recipe) { expandedInstances.Clear(); areRowsStale = true; }
            recipe = setRecipe;
            totalTime = Mathf.Max(setTotalTime, 0.01f);
            time = setTime;
            if (AreRowsStale()) { CollectRows(); }
            
            int newLayoutHash = GetLayoutHash();
            if (newLayoutHash != layoutHash || playhead == null)
            {
                layoutHash = newLayoutHash;
                Rebuild();
            }
            
            UpdateTime();
        }

        public bool IsCollapsed() => isCollapsed;

        public void SetCollapsed(bool setCollapsed)
        {
            if (isCollapsed == setCollapsed) { return; }
            isCollapsed = setCollapsed;
            Refresh(recipe, time, totalTime);
        }

        public void MarkRowsStale()
        {
            // e.g. preview visibility changed (not part of the recipe's revision)
            areRowsStale = true;
        }
        #endregion
        
        #region EventHandlers
        private void HandlePointerDown(PointerDownEvent pointerDownEvent)
        {
            // The overall label collapses / expands the timeline, a header's label toggles its instance (no scrub)
            if (pointerDownEvent.localPosition.x < labelWidth)
            {
                int rowIndex = Mathf.FloorToInt(pointerDownEvent.localPosition.y / _rowHeight);
                if (rowIndex == 0 && recipe != null)
                {
                    ToggleCollapsed();
                    return;
                }
                if (TryToggleRow(rowIndex - 1)) { return; }
            }
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
            bool wasScrubbing = isScrubbing;
            isScrubbing = false;
            this.ReleasePointer(pointerUpEvent.pointerId);
            if (wasScrubbing) { scrubEnded?.Invoke(); }
        }
        #endregion

        #region ScrubAndPaint
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
        
        private void UpdateTime()
        {
            if (playhead == null) { return; }
            float trackWidth = Mathf.Max(contentRect.width - labelWidth, 0f);
            playhead.style.left = labelWidth + Mathf.Clamp01(time / totalTime) * trackWidth;
            for (int i = 0; i < overallBars.Count; i++)
            {
                bool isActive = BattleEffectRecipeEntry.IsActive(overallSpans[i].x, overallSpans[i].y, time);
                overallBars[i].style.backgroundColor = isActive ? BattleEffectEntryColours.activeOverall : BattleEffectEntryColours.overall;
            }
            for (int i = 0; i < bars.Count; i++)
            {
                BattleEffectTimelineRow row = rows[i];
                bool isActive = BattleEffectRecipeEntry.IsActive(row.startTime, row.endTime, time);
                bars[i].style.backgroundColor = row.isHidden ? (isActive ? BattleEffectEntryColours.activeHidden : BattleEffectEntryColours.hidden)
                    : row.isHeader ? (isActive ? BattleEffectEntryColours.activeInstance : BattleEffectEntryColours.instance)
                    : (isActive ? BattleEffectEntryColours.activeItem : BattleEffectEntryColours.item);
            }
        }
        
        private void Rebuild()
        {
            Clear();
            bars.Clear();
            overallBars.Clear();
            playhead = null;
            if (recipe == null) { return; }

            Add(BuildOverallRow());
            if (!isCollapsed)
            {
                foreach (BattleEffectTimelineRow row in rows) { Add(BuildRow(row)); }
            }
            style.height = (isCollapsed ? 1 : rows.Count + 1) * _rowHeight;

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
        #endregion
        
        #region RowBehaviour
        internal void ToggleCollapsed()
        {
            SetCollapsed(!isCollapsed);
            collapseToggled?.Invoke(isCollapsed);
        }

        internal bool TryToggleRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= rows.Count || !rows[rowIndex].isHeader) { return false; }
            string instancePath = rows[rowIndex].instancePath;
            if (!expandedInstances.Remove(instancePath)) { expandedInstances.Add(instancePath); }
            areRowsStale = true;
            Refresh(recipe, time, totalTime);
            return true;
        }

        private bool AreRowsStale()
        {
            if (recipe == null) { return rows.Count > 0; }
            return areRowsStale || recipe.GetRevision() != collectedRevision || recipe.GetInstanceRevision() != collectedInstanceRevision;
        }

        private void CollectRows()
        {
            areRowsStale = false;
            collectedRevision = recipe != null ? recipe.GetRevision() : -1;
            collectedInstanceRevision = recipe != null ? recipe.GetInstanceRevision() : -1;
            rows.Clear();
            overallSpans.Clear();
            if (recipe == null) { return; }
            rows.AddRange(CollectRows(recipe, 0f, 0, "", false));

            var itemSpans = new List<Vector2>();
            CollectShownItemSpans(recipe, 0f, 0, itemSpans);
            overallSpans.AddRange(MergeSpans(itemSpans));
        }

        private List<BattleEffectTimelineRow> CollectRows(BattleEffectRecipe rowRecipe, float offset, int depth, string pathPrefix, bool isParentHidden)
        {
            // Recipe's entries in timeline order (per BattleEffectRecipeEntries) ~ rows are either A. items or B. instance header w/ child items below
            var sortedRows = new List<BattleEffectTimelineRow>();
            foreach (BattleEffectRecipeEntry entry in BattleEffectRecipeEntries.Collect(rowRecipe, depth < BattleEffectRecipe.maxInstanceDepth))
            {
                bool isHidden = isParentHidden || BattleEffectPreviewVisibility.IsHidden(rowRecipe, entry.id);
                if (!entry.isInstance)
                {
                    sortedRows.Add(new BattleEffectTimelineRow(entry.label, offset + entry.startTime, offset + entry.endTime, depth, null, false, isHidden));
                    continue;
                }
                BattleEffectInstance instance = rowRecipe.GetInstances()[entry.index];
                if (instance.GetRecipe() == null) { continue; }
                string instancePath = pathPrefix + entry.index;
                bool isExpanded = expandedInstances.Contains(instancePath);
                sortedRows.Add(new BattleEffectTimelineRow(entry.label, offset + entry.startTime, offset + entry.endTime, depth, instancePath, isExpanded, isHidden));
                if (isExpanded) { sortedRows.AddRange(CollectRows(instance.GetRecipe(), offset + instance.GetStartOffset(), depth + 1, instancePath + "/", isHidden)); }
            }
            return sortedRows;
        }
        
        private static void CollectShownItemSpans(BattleEffectRecipe spanRecipe, float offset, int depth, List<Vector2> spans)
        {
            // Every shown item (instances flattened regardless of expansion)
            foreach (BattleEffectRecipeEntry entry in BattleEffectRecipeEntries.Collect(spanRecipe, depth < BattleEffectRecipe.maxInstanceDepth))
            {
                if (BattleEffectPreviewVisibility.IsHidden(spanRecipe, entry.id)) { continue; }
                if (!entry.isInstance)
                {
                    spans.Add(new Vector2(offset + entry.startTime, offset + entry.endTime));
                    continue;
                }
                BattleEffectInstance instance = spanRecipe.GetInstances()[entry.index];
                if (instance.GetRecipe() != null) { CollectShownItemSpans(instance.GetRecipe(), offset + instance.GetStartOffset(), depth + 1, spans); }
            }
        }

        internal static List<Vector2> MergeSpans(IEnumerable<Vector2> spans)
        {
            var merged = new List<Vector2>();
            foreach (Vector2 span in spans.OrderBy(span => span.x))
            {
                if (merged.Count > 0 && span.x <= merged[^1].y) { merged[^1] = new Vector2(merged[^1].x, Mathf.Max(merged[^1].y, span.y)); }
                else { merged.Add(span); }
            }
            return merged;
        }

        private VisualElement BuildOverallRow()
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexDirection = FlexDirection.Row, height = _rowHeight } };
            Label label = CreateRowLabel($"{(isCollapsed ? "+" : "-")} {_overallLabel}", 0, true);
            label.tooltip = $"When any shown item/instance plays:  click to {(isCollapsed ? "expand" : "collapse")} the timeline";
            row.Add(label);

            var track = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexGrow = 1f, backgroundColor = _trackColour } };
            foreach (Vector2 span in overallSpans)
            {
                VisualElement bar = CreateBar(span.x, span.y);
                overallBars.Add(bar);
                track.Add(bar);
            }
            row.Add(track);
            return row;
        }

        private VisualElement BuildRow(BattleEffectTimelineRow timelineRow)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexDirection = FlexDirection.Row, height = _rowHeight } };
            string label = timelineRow.isHeader ? $"{(timelineRow.isExpanded ? "-" : "+")} {timelineRow.label}" : timelineRow.label;
            Label rowLabel = CreateRowLabel(label, timelineRow.depth, timelineRow.isHeader);
            rowLabel.tooltip = timelineRow.isHeader ? $"{timelineRow.label} (instance:  click to {(timelineRow.isExpanded ? "collapse" : "expand")})" : timelineRow.label;
            if (timelineRow.isHidden) { rowLabel.style.opacity = 0.5f; }
            row.Add(rowLabel);

            var track = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexGrow = 1f, backgroundColor = _trackColour } };
            VisualElement bar = CreateBar(timelineRow.startTime, timelineRow.endTime);
            bars.Add(bar);
            track.Add(bar);
            row.Add(track);
            return row;
        }

        private static Label CreateRowLabel(string text, int depth, bool isBold)
        {
            return new Label(text)
            {
                style =
                {
                    width = labelWidth,
                    flexShrink = 0f,
                    paddingLeft = depth * _indentWidth,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal,
                    overflow = Overflow.Hidden,
                    whiteSpace = WhiteSpace.NoWrap,
                    textOverflow = TextOverflow.Ellipsis
                }
            };
        }

        private VisualElement CreateBar(float startTime, float endTime)
        {
            return new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = _barInset,
                    bottom = _barInset,
                    left = new Length(Mathf.Clamp01(startTime / totalTime) * 100f, LengthUnit.Percent),
                    width = new Length(Mathf.Clamp01((endTime - startTime) / totalTime) * 100f, LengthUnit.Percent)
                }
            };
        }
        #endregion
        
        #region Utility
        private int GetLayoutHash()
        {
            if (recipe == null) { return 0; }
            int hash = recipe.GetHashCode() ^ totalTime.GetHashCode();
            hash = hash * 31 + (isCollapsed ? 1 : 0);
            foreach (Vector2 span in overallSpans) { hash = hash * 31 + span.GetHashCode(); }
            foreach (BattleEffectTimelineRow row in rows)
            {
                hash = hash * 31 + (row.label?.GetHashCode() ?? 0);
                hash = hash * 31 + row.startTime.GetHashCode();
                hash = hash * 31 + row.endTime.GetHashCode();
                hash = hash * 31 + (row.instancePath?.GetHashCode() ?? 0) + (row.isExpanded ? 1 : 0) + (row.isHidden ? 2 : 0);
            }
            return hash;
        }
        #endregion
    }
}
#endif
