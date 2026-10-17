#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    public sealed class BattleEffectTimelineElement : VisualElement
    {
        // Structure / Behaviour Overview:
        //  - One bar per recipe item over its time window w/ a playhead
        //      - Clicking / dragging scrubs through the playback
        //  - Rows are only rebuilt when the items' layout changes (labels / timings)
        //      - Otherwise just the playhead + highlights move

        // Const Tunables
        private const float _rowHeight = 16f;
        private const float _labelWidth = 110f;
        private const float _barInset = 2f;
        private const float _playheadWidth = 2f;
        private static readonly Color _barColour = new(0.35f, 0.55f, 0.85f, 1f);
        private static readonly Color _activeBarColour = new(0.55f, 0.8f, 1f, 1f);
        private static readonly Color _playheadColour = new(1f, 0.4f, 0.3f, 1f);
        private static readonly Color _trackColour = new(0f, 0f, 0f, 0.25f);

        // State
        private BattleEffectRecipe recipe;
        private float totalTime = 1f;
        private float time;
        private bool isScrubbing = false;
        private int layoutHash;
        private VisualElement playhead;
        private readonly List<(VisualElement bar, BattleEffectItem item)> bars = new();

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
        public void Refresh(BattleEffectRecipe setRecipe, float setTotalTime, float setTime)
        {
            float clampedTotalTime = Mathf.Max(setTotalTime, 0.01f);
            int newLayoutHash = GetLayoutHash(setRecipe, clampedTotalTime);
            recipe = setRecipe;
            totalTime = clampedTotalTime;
            time = setTime;
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
        private void Rebuild()
        {
            Clear();
            bars.Clear();
            playhead = null;
            if (recipe == null) { return; }

            int index = 0;
            foreach (BattleEffectItem item in recipe.GetItems())
            {
                if (item == null) { continue; }
                Add(BuildRow(item, index));
                index++;
            }
            style.height = index * _rowHeight;

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
            float trackWidth = Mathf.Max(contentRect.width - _labelWidth, 0f);
            playhead.style.left = _labelWidth + Mathf.Clamp01(time / totalTime) * trackWidth;
            foreach ((VisualElement bar, BattleEffectItem item) in bars)
            {
                bool isActive = time >= item.GetStartTime() && time <= item.GetEndTime();
                bar.style.backgroundColor = isActive ? _activeBarColour : _barColour;
            }
        }

        private VisualElement BuildRow(BattleEffectItem item, int index)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexDirection = FlexDirection.Row, height = _rowHeight } };
            string label = string.IsNullOrEmpty(item.GetLabel()) ? $"Item {index}" : item.GetLabel();
            row.Add(new Label(label)
            {
                tooltip = label,
                style =
                {
                    width = _labelWidth,
                    flexShrink = 0f,
                    unityTextAlign = TextAnchor.MiddleLeft,
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
                    left = new Length(Mathf.Clamp01(item.GetStartTime() / totalTime) * 100f, LengthUnit.Percent),
                    width = new Length(Mathf.Clamp01((item.GetEndTime() - item.GetStartTime()) / totalTime) * 100f, LengthUnit.Percent)
                }
            };
            bars.Add((bar, item));
            track.Add(bar);
            row.Add(track);
            return row;
        }

        private void Scrub(Vector2 localPosition)
        {
            float trackWidth = contentRect.width - _labelWidth;
            if (trackWidth <= 0f) { return; }
            scrubbed?.Invoke(Mathf.Clamp01((localPosition.x - _labelWidth) / trackWidth) * totalTime);
        }
        
        private static int GetLayoutHash(BattleEffectRecipe recipe, float totalTime)
        {
            if (recipe == null) { return 0; }
            int hash = recipe.GetHashCode() ^ totalTime.GetHashCode();
            foreach (BattleEffectItem item in recipe.GetItems())
            {
                if (item == null) { continue; }
                hash = hash * 31 + (item.GetLabel()?.GetHashCode() ?? 0);
                hash = hash * 31 + item.GetStartTime().GetHashCode();
                hash = hash * 31 + item.GetEndTime().GetHashCode();
            }
            return hash;
        }
        #endregion
    }
}
#endif
