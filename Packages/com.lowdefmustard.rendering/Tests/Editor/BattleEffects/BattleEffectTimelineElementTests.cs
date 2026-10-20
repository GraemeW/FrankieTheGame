using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Rendering.Editor;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectTimelineElementTests
    {
        // Notes:
        //  - Structure is read back w/o a panel (rows, labels, bar styles)
        //  - Scrub maths are primarily tested via the internal TryGetScrubTime

        // State
        private BattleEffectRecipe recipe;
        private BattleEffectTimelineElement timeline;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            recipe = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            recipe.SetItems(new[]
            {
                CreateItem(new BattleEffectCircle(), "{\"label\":\"Pop\",\"startTimeSeconds\":0,\"durationSeconds\":1}"),
                null,
                CreateItem(new BattleEffectDiamond(), "{\"startTimeSeconds\":1,\"durationSeconds\":1}")
            });
            timeline = new BattleEffectTimelineElement();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(recipe);
        }
        #endregion

        #region PrivateMethods
        private static VisualElement GetBar(VisualElement row) => row.ElementAt(1).ElementAt(0);
        #endregion
        
        #region Structure
        [Test]
        public void Refresh_BuildsOneRowPerItemPlusThePlayhead()
        {
            timeline.Refresh(recipe, 0f, 2f);

            Assert.AreEqual(3, timeline.childCount, "2 rows (null item skipped) + playhead");
            Assert.AreEqual("Pop", timeline.ElementAt(0).Q<Label>().text);
            Assert.AreEqual("Item 1", timeline.ElementAt(1).Q<Label>().text, "unlabelled items are numbered by row");
        }

        [Test]
        public void Refresh_BarsSpanTheirItemWindowAsAPercentOfTotalTime()
        {
            timeline.Refresh(recipe, 0f, 2f);

            VisualElement secondBar = GetBar(timeline.ElementAt(1));
            Assert.AreEqual(50f, secondBar.style.left.value.value, 1e-4f);
            Assert.AreEqual(LengthUnit.Percent, secondBar.style.left.value.unit);
            Assert.AreEqual(50f, secondBar.style.width.value.value, 1e-4f);
        }

        [Test]
        public void Refresh_HighlightsOnlyActiveItems()
        {
            timeline.Refresh(recipe, 0.5f, 2f);
            Color firstBarAt05 = GetBar(timeline.ElementAt(0)).style.backgroundColor.value;
            Color secondBarAt05 = GetBar(timeline.ElementAt(1)).style.backgroundColor.value;

            timeline.Refresh(recipe, 1.5f, 2f);

            Assert.AreNotEqual(firstBarAt05, secondBarAt05);
            Assert.AreEqual(secondBarAt05, GetBar(timeline.ElementAt(0)).style.backgroundColor.value, "first item now inactive");
            Assert.AreEqual(firstBarAt05, GetBar(timeline.ElementAt(1)).style.backgroundColor.value, "second item now active");
        }

        [Test]
        public void Refresh_SameLayout_KeepsTheRows()
        {
            timeline.Refresh(recipe, 0f, 2f);
            VisualElement firstRow = timeline.ElementAt(0);

            timeline.Refresh(recipe, 1f, 2f);

            Assert.AreSame(firstRow, timeline.ElementAt(0));
        }

        [Test]
        public void Refresh_ChangedTimings_RebuildsTheRows()
        {
            timeline.Refresh(recipe, 0f, 2f);
            VisualElement firstRow = timeline.ElementAt(0);

            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"startTimeSeconds\":0.5,\"durationSeconds\":1}") });
            timeline.Refresh(recipe, 0f, 2f);

            Assert.AreNotSame(firstRow, timeline.ElementAt(0));
            Assert.AreEqual(2, timeline.childCount);
        }

        [Test]
        public void Refresh_NoRecipe_IsEmpty()
        {
            timeline.Refresh(recipe, 0f, 2f);

            timeline.Refresh(null, 0f, 2f);

            Assert.AreEqual(0, timeline.childCount);
        }
        #endregion

        #region Scrub
        [TestCase(BattleEffectTimelineElement.labelWidth, 0f)]
        [TestCase(BattleEffectTimelineElement.labelWidth + 50f, 1f)]
        [TestCase(BattleEffectTimelineElement.labelWidth + 100f, 2f)]
        [TestCase(0f, 0f)]
        [TestCase(1000f, 2f)]
        public void TryGetScrubTime_MapsTheTrackOntoTotalTime(float localX, float expectedTime)
        {
            Assert.IsTrue(BattleEffectTimelineElement.TryGetScrubTime(localX, BattleEffectTimelineElement.labelWidth + 100f, 2f, out float scrubTime));
            Assert.AreEqual(expectedTime, scrubTime, 1e-4f);
        }

        [Test]
        public void TryGetScrubTime_NoTrackWidth_ReturnsFalse()
        {
            Assert.IsFalse(BattleEffectTimelineElement.TryGetScrubTime(50f, BattleEffectTimelineElement.labelWidth, 2f, out _));
        }

        [UnityTest]
        public IEnumerator PointerDown_InAShownWindow_RaisesScrubbed()
        {
            var window = new HeadlessEditorWindow(width: BattleEffectTimelineElement.labelWidth + 200f);
            try
            {
                timeline.style.width = BattleEffectTimelineElement.labelWidth + 200f;
                window.root.Add(timeline);
                timeline.Refresh(recipe, 0f, 2f);
                float? scrubbedTime = null;
                timeline.scrubbed += scrubTime => scrubbedTime = scrubTime;
                yield return HeadlessEditorWindow.WaitUntil(() => timeline.contentRect.width > BattleEffectTimelineElement.labelWidth);

                Vector2 pressPosition = timeline.LocalToWorld(new Vector2(BattleEffectTimelineElement.labelWidth + 100f, 4f));
                using (PointerDownEvent pointerDownEvent = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = pressPosition, button = 0 }))
                {
                    pointerDownEvent.target = timeline;
                    timeline.SendEvent(pointerDownEvent);
                }
                using (PointerUpEvent pointerUpEvent = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = pressPosition, button = 0 }))
                {
                    pointerUpEvent.target = timeline;
                    timeline.SendEvent(pointerUpEvent);
                }
                yield return HeadlessEditorWindow.WaitUntil(() => scrubbedTime.HasValue);

                Assert.IsTrue(scrubbedTime.HasValue, "scrubbed was raised");
                Assert.AreEqual(1f, scrubbedTime.Value, 0.05f, "half way along the track");
            }
            finally { window.Close(); }
        }
        #endregion
    }
}
