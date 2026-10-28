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
        
        private BattleEffectRecipe CreateInstancedRecipe(out BattleEffectRecipe grandchild)
        {
            // The setup recipe + an instance (offset 2s) of a child w/ one item (0.5 -> 1s) + its own instance (offset 0.25s) of a grandchild
            grandchild = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            grandchild.name = "Grandchild";
            grandchild.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"label\":\"Spark\",\"startTimeSeconds\":0,\"durationSeconds\":0.5}") });
            var child = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            child.name = "Child";
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"label\":\"Ring\",\"startTimeSeconds\":0.5,\"durationSeconds\":0.5}") });
            child.SetInstances(new[] { new BattleEffectInstance(grandchild, 0.25f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });
            recipe.SetInstances(new[] { new BattleEffectInstance(child, 2f, true, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });
            return child;
        }
        #endregion
        
        #region Structure
        [Test]
        public void Refresh_BuildsOneRowPerItemPlusThePlayhead()
        {
            timeline.Refresh(recipe, 0f, 2f);

            Assert.AreEqual(3, timeline.childCount, "2 rows (null item skipped) + playhead");
            Assert.AreEqual("Pop", timeline.ElementAt(0).Q<Label>().text);
            Assert.AreEqual("Item 2", timeline.ElementAt(1).Q<Label>().text, "unlabelled items are numbered by their list index");
        }

        [Test]
        public void Refresh_RecollectsRowsOnlyOnARevisionChange()
        {
            // Every playback tick refreshes:  rows (+ their allocations) are only collected again once the recipe changes
            timeline.Refresh(recipe, 0f, 2f);
            JsonUtility.FromJsonOverwrite("{\"label\":\"Renamed\"}", recipe.GetItems()[0]); // In place:  no revision change

            timeline.Refresh(recipe, 0.5f, 2f);
            Assert.AreEqual("Pop", timeline.ElementAt(0).Q<Label>().text, "cached");

            recipe.SetItems(recipe.GetItems());
            timeline.Refresh(recipe, 0.5f, 2f);
            Assert.AreEqual("Renamed", timeline.ElementAt(0).Q<Label>().text, "recollected after the revision bump");
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

        #region Instances
        [Test]
        public void Refresh_Instance_AddsACollapsedHeaderSpanningItsItems()
        {
            BattleEffectRecipe child = CreateInstancedRecipe(out BattleEffectRecipe grandchild);
            try
            {
                timeline.Refresh(recipe, 0f, 4f);

                Assert.AreEqual(4, timeline.childCount, "2 item rows + 1 header + playhead");
                Assert.AreEqual("+ Child", timeline.ElementAt(2).Q<Label>().text, "unlabelled instances use the recipe's name");
                VisualElement headerBar = GetBar(timeline.ElementAt(2));
                Assert.AreEqual(2.25f / 4f * 100f, headerBar.style.left.value.value, 1e-3f, "from the grandchild's spark (2 + 0.25 + 0)");
                Assert.AreEqual(0.75f / 4f * 100f, headerBar.style.width.value.value, 1e-3f, "to the ring's end (2 + 1)");
            }
            finally { Object.DestroyImmediate(child); Object.DestroyImmediate(grandchild); }
        }

        [Test]
        public void TryToggleRow_Header_ExpandsItsReTimedItemsThenCollapses()
        {
            BattleEffectRecipe child = CreateInstancedRecipe(out BattleEffectRecipe grandchild);
            try
            {
                timeline.Refresh(recipe, 0f, 4f);

                Assert.IsTrue(timeline.TryToggleRow(2));
                Assert.AreEqual(6, timeline.childCount, "+ the grandchild's header + the ring");
                Assert.AreEqual("- Child", timeline.ElementAt(2).Q<Label>().text);
                Assert.AreEqual("+ Grandchild", timeline.ElementAt(3).Q<Label>().text, "nested instances start collapsed + sort by start (2.25s)");
                Label ringLabel = timeline.ElementAt(4).Q<Label>();
                Assert.AreEqual("Ring", ringLabel.text);
                Assert.Greater(ringLabel.style.paddingLeft.value.value, 0f, "indented");
                Assert.AreEqual(2.5f / 4f * 100f, GetBar(timeline.ElementAt(4)).style.left.value.value, 1e-3f, "re-timed by the offset");

                Assert.IsTrue(timeline.TryToggleRow(3));
                Assert.AreEqual("Spark", timeline.ElementAt(4).Q<Label>().text, "the grandchild's rows stay under its header");
                Assert.AreEqual(2.25f / 4f * 100f, GetBar(timeline.ElementAt(4)).style.left.value.value, 1e-3f, "both offsets");
                Assert.AreEqual("Ring", timeline.ElementAt(5).Q<Label>().text);

                Assert.IsTrue(timeline.TryToggleRow(2));
                Assert.AreEqual(4, timeline.childCount, "collapsed again");
            }
            finally { Object.DestroyImmediate(child); Object.DestroyImmediate(grandchild); }
        }

        [Test]
        public void Refresh_Instance_SortsAmongTheItemsByStartTime()
        {
            var child = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            child.name = "Child";
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"startTimeSeconds\":0,\"durationSeconds\":0.25}") });
            recipe.SetInstances(new[] { new BattleEffectInstance(child, 0.5f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });
            try
            {
                timeline.Refresh(recipe, 0f, 2f);

                Assert.AreEqual("Pop", timeline.ElementAt(0).Q<Label>().text, "0s");
                Assert.AreEqual("+ Child", timeline.ElementAt(1).Q<Label>().text, "0.5s");
                Assert.AreEqual("Item 2", timeline.ElementAt(2).Q<Label>().text, "1s");
            }
            finally { Object.DestroyImmediate(child); }
        }

        [Test]
        public void Refresh_SameStart_SortsTheEarlierEndFirst()
        {
            recipe.SetItems(new[]
            {
                CreateItem(new BattleEffectCircle(), "{\"label\":\"Long\",\"startTimeSeconds\":1,\"durationSeconds\":4}"),
                CreateItem(new BattleEffectCircle(), "{\"label\":\"Short\",\"startTimeSeconds\":1,\"durationSeconds\":3}")
            });

            timeline.Refresh(recipe, 0f, 5f);

            Assert.AreEqual("Short", timeline.ElementAt(0).Q<Label>().text, "1 -> 4s");
            Assert.AreEqual("Long", timeline.ElementAt(1).Q<Label>().text, "1 -> 5s");
        }

        [Test]
        public void TryToggleRow_ItemRowOrOutOfRange_IsIgnored()
        {
            timeline.Refresh(recipe, 0f, 2f);

            Assert.IsFalse(timeline.TryToggleRow(0));
            Assert.IsFalse(timeline.TryToggleRow(9));
            Assert.AreEqual(3, timeline.childCount);
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
