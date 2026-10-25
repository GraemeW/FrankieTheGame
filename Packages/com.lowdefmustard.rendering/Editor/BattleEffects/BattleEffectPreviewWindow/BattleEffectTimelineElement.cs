#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    public sealed class BattleEffectTimelineElement : VisualElement
    {
        // Structure / Behaviour Overview:
        //  - One bar per recipe item over its time window w/ a playhead
        //      - Clicking / dragging scrubs through the playback
        //  - Each instance adds a header row (its bar spans its items) collapsed by default ~ when expanded, its rows stay under its header
        //  - Rows (items + instance headers) are sorted by start time, then end time
        //  - Rows are only rebuilt when the rows' layout changes (labels / timings / expanded instances)
        // Note:  Internal methods/properties are for test visibility

        // Const Tunables
        private const float _rowHeight = 16f;
        internal const float labelWidth = 110f;
        private const float _barInset = 2f;
        private const float _playheadWidth = 2f;
        private const float _indentWidth = 10f;
        private static readonly Color _playheadColour = new(1f, 0.4f, 0.3f, 1f);
        private static readonly Color _trackColour = new(0f, 0f, 0f, 0.25f);

        // State
        private BattleEffectRecipe recipe;
        
        private float totalTime = 1f;
        private float time;
        private bool isScrubbing = false;
        
        private int layoutHash;
        private readonly HashSet<string> expandedInstances = new();
        
        private bool areRowsStale = true;
        private int collectedRevision = -1;
        private int collectedInstanceRevision = -1;
        
        // Element References
        private VisualElement playhead;
        private readonly List<BattleEffectTimelineRow> rows = new();
        private readonly List<VisualElement> bars = new();

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
            for (int i = 0; i < bars.Count; i++)
            {
                BattleEffectTimelineRow row = rows[i];
                bool isActive = BattleEffectRecipeEntry.IsActive(row.startTime, row.endTime, time);
                bars[i].style.backgroundColor = row.isHeader
                    ? (isActive ? BattleEffectEntryColours.activeInstance : BattleEffectEntryColours.instance)
                    : (isActive ? BattleEffectEntryColours.activeItem : BattleEffectEntryColours.item);
            }
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
        #endregion
        
        #region RowBehaviour
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
            if (recipe != null) { rows.AddRange(CollectRows(recipe, 0f, 0, "")); }
        }

        private List<BattleEffectTimelineRow> CollectRows(BattleEffectRecipe rowRecipe, float offset, int depth, string pathPrefix)
        {
            // Recipe's entries in timeline order (per BattleEffectRecipeEntries) ~ rows are either A. items or B. instance header w/ child items below
            var sortedRows = new List<BattleEffectTimelineRow>();
            foreach (BattleEffectRecipeEntry entry in BattleEffectRecipeEntries.Collect(rowRecipe, depth < BattleEffectRecipe.maxInstanceDepth))
            {
                if (!entry.isInstance)
                {
                    sortedRows.Add(new BattleEffectTimelineRow(entry.label, offset + entry.startTime, offset + entry.endTime, depth, null, false));
                    continue;
                }
                BattleEffectInstance instance = rowRecipe.GetInstances()[entry.index];
                if (instance.GetRecipe() == null) { continue; }
                string instancePath = pathPrefix + entry.index;
                bool isExpanded = expandedInstances.Contains(instancePath);
                sortedRows.Add(new BattleEffectTimelineRow(entry.label, offset + entry.startTime, offset + entry.endTime, depth, instancePath, isExpanded));
                if (isExpanded) { sortedRows.AddRange(CollectRows(instance.GetRecipe(), offset + instance.GetStartOffset(), depth + 1, instancePath + "/")); }
            }
            return sortedRows;
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
        #endregion
        
        #region Utility
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
