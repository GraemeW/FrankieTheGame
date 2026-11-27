using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using LowDefMustard.Utils.Tests;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZoneViewerTests
    {
        // State
        private readonly TestObjectTracker tracker = new();
        private MultiZoneViewer viewer;
        private bool attached;
        private Zone zone;
        private ZoneViewData zoneViewData;
        private MultiZoneView multiZoneView;
        private ZoneViewData unrelatedZoneViewData;
        private Object originalSelection;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalSelection = Selection.activeObject;
            viewer = ScriptableObject.CreateInstance<MultiZoneViewer>();
            attached = false;
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = originalSelection;
            if (zoneViewData != null) { Undo.ClearUndo(zoneViewData); }
            if (unrelatedZoneViewData != null) { Undo.ClearUndo(unrelatedZoneViewData); }
            tracker.DestroyAll();
            if (viewer != null)
            {
                if (attached) { HeadlessEditorWindow.Close(viewer); }
                else { Object.DestroyImmediate(viewer); }
            }
        }

        private void Attach()
        {
            viewer.ShowUtility();
            viewer.position = new Rect(-10000, -10000, 400, 300);
            attached = true;
        }

        private void ShowInMemoryView()
        {
            zoneViewData = tracker.Create<ZoneViewData>();
            multiZoneView = tracker.Create<MultiZoneView>();
            multiZoneView.zoneViewDataSet.Add(zoneViewData);
            viewer.activeMultiZoneView = multiZoneView;
            viewer.CreateGUI();
            viewer.isDraggingNodeLink = true; // Cleared by a rebuild ~ proxy for us to tell whether undo rebuilt the views
        }

        private static void RecordMove(ZoneViewData target, Vector2 newPosition)
        {
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(target, "MultiZoneViewerTests Move");
            target.topLeftPosition = newPosition;
            EditorUtility.SetDirty(target);
            Undo.IncrementCurrentGroup();
        }
        #endregion

        #region Tests
        [Test]
        public void Undo_ZoneViewMoved_RebuildsViews()
        {
            ShowInMemoryView();
            RecordMove(zoneViewData, new Vector2(40f, 40f));

            Undo.PerformUndo();

            Assert.AreEqual(Vector2.zero, zoneViewData.topLeftPosition);
            Assert.IsFalse(viewer.isDraggingNodeLink, "views rebuilt");
        }

        [Test]
        public void Undo_UnrelatedObject_KeepsViews()
        {
            ShowInMemoryView();
            unrelatedZoneViewData = tracker.Create<ZoneViewData>();
            RecordMove(unrelatedZoneViewData, new Vector2(40f, 40f));

            Undo.PerformUndo();

            Assert.IsTrue(viewer.isDraggingNodeLink, "views not rebuilt");
        }

        [Test]
        public void CreateGUI_ActiveMultiZoneViewNull_BuildsToolbarAndCanvasWithoutLoadingAnything()
        {
            viewer.activeMultiZoneView = null;

            viewer.CreateGUI();

            Assert.IsNotNull(viewer.canvas);
            Assert.IsNotNull(viewer.statusLabel);
            Assert.IsNotNull(viewer.clearButton);
            Assert.IsNotNull(viewer.zoomLabel);
            Assert.IsNotNull(viewer.multiZoneViewField);
            Assert.AreEqual(0, viewer.zoneViews.Count);
        }

        [Test]
        public void RefreshToolbarState_NoZoneViews_DisablesClearButtonAndClearsStatusText()
        {
            viewer.CreateGUI();

            viewer.RefreshToolbarState();

            Assert.IsFalse(viewer.clearButton.enabledSelf);
            Assert.AreEqual(string.Empty, viewer.statusLabel.text);
        }

        [Test]
        public void RefreshToolbarState_WithZoneViews_EnablesClearButtonAndSetsStatusText()
        {
            viewer.CreateGUI();
            zoneViewData = tracker.Create<ZoneViewData>();
            viewer.zoneViews.Add(new ZoneView(zoneViewData, null, Vector2.zero, Vector2.zero));

            viewer.RefreshToolbarState();

            Assert.IsTrue(viewer.clearButton.enabledSelf);
            Assert.AreEqual("1 scene(s)  \u2014  middle-click or alt+drag to pan, scroll to zoom", viewer.statusLabel.text);
        }

        [Test]
        public void OnPlayModeStateChanged_EnteredPlayMode_MarksToolUnavailable()
        {
            viewer.CreateGUI();

            viewer.OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);

            Assert.IsFalse(viewer.isToolAvailable);
        }

        [Test]
        public void OnPlayModeStateChanged_EnteredEditMode_MarksToolAvailable()
        {
            viewer.CreateGUI();
            viewer.isToolAvailable = false;

            viewer.OnPlayModeStateChanged(PlayModeStateChange.EnteredEditMode);

            Assert.IsTrue(viewer.isToolAvailable);
        }

        [Test]
        public void OnSelectionChanged_ToolNotAvailable_DoesNotMovePanOffset()
        {
            viewer.CreateGUI();
            viewer.isToolAvailable = false;
            Vector2 originalPanOffset = viewer.panOffset;
            zone = tracker.Create<Zone>("ZoneA");
            zone.preventLocalizationForTests = true;

            Selection.activeObject = zone;
            viewer.OnSelectionChanged();

            Assert.AreEqual(originalPanOffset, viewer.panOffset);
        }

        [Test]
        public void OnSelectionChanged_ZoneWithNoMatchingZoneView_DoesNotMovePanOffset()
        {
            viewer.CreateGUI();
            Vector2 originalPanOffset = viewer.panOffset;
            zone = tracker.Create<Zone>("ZoneWithNoView");
            zone.preventLocalizationForTests = true;

            Selection.activeObject = zone;
            viewer.OnSelectionChanged();

            Assert.AreEqual(originalPanOffset, viewer.panOffset);
        }

        [UnityTest]
        public System.Collections.IEnumerator OnSelectionChanged_ZoneWithMatchingZoneView_CentersPanOffsetOnZone()
        {
            viewer.CreateGUI();
            Attach();
            yield return HeadlessEditorWindow.WaitUntilSettled(viewer);

            zone = tracker.Create<Zone>("ZoneA");
            zone.preventLocalizationForTests = true;
            zoneViewData = tracker.Create<ZoneViewData>();
            zoneViewData.Setup("ZoneA", "Assets/ZoneA.unity", "snap.png", new Vector2(100f, 80f), new Vector2(10f, 20f));
            var zoneView = new ZoneView(zoneViewData, null, Vector2.zero, Vector2.zero);
            viewer.zoneViewLookup["ZoneA"] = zoneView;

            Selection.activeObject = zone;
            viewer.OnSelectionChanged();
            yield return null;

            // boxCentre = topLeftPosition (10,20) + (dimensions.x/2, (headerHeight 24 + dimensions.y)/2) = (10,20) + (50,52) = (60,72)
            var boxCentre = new Vector2(60f, 72f);
            Vector2 expectedPanOffset = viewer.canvas.contentRect.size / 2f - boxCentre * viewer.zoomScale;
            Assert.AreEqual(expectedPanOffset.x, viewer.panOffset.x, 0.5f);
            Assert.AreEqual(expectedPanOffset.y, viewer.panOffset.y, 0.5f);
        }

        [Test]
        public void OnSelectionChanged_ZoneNodeWithMatchingZoneViewAndNodeData_CentersPanOffsetOnNode()
        {
            viewer.CreateGUI();

            var zoneNode = tracker.Create<ZoneNode>("node-1");
            zoneNode.preventLocalizationForTests = true;
            zoneNode.SetZoneName("ZoneA");

            zoneViewData = tracker.Create<ZoneViewData>();
            zoneViewData.Setup("ZoneA", "Assets/ZoneA.unity", "snap.png", new Vector2(100f, 80f), Vector2.zero);
            zoneViewData.SetZoneNodeData(new List<ZoneNodeData> { new("node-1", new Vector2(0.5f, 0.5f)) });
            var zoneView = new ZoneView(zoneViewData, null, Vector2.zero, Vector2.zero);
            viewer.zoneViewLookup["ZoneA"] = zoneView;
            Vector2 originalPanOffset = viewer.panOffset;

            Selection.activeObject = zoneNode;
            viewer.OnSelectionChanged();

            // Relative position (0.5,0.5) is a real node, so this should move away from the untouched default
            Assert.AreNotEqual(originalPanOffset, viewer.panOffset);
        }
        #endregion
    }
}
