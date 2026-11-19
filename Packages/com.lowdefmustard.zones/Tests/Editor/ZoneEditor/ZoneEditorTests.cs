using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using LowDefMustard.Utils.Tests;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneEditorTests
    {
        // State
        private ZoneEditor editor;
        private Zone zone;
        private Object originalSelection;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalSelection = Selection.activeObject;
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            zone.name = "MyZone";

            editor = ScriptableObject.CreateInstance<ZoneEditor>();
            editor.ShowUtility();
            editor.position = new Rect(-10000, -10000, 400, 300);
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = originalSelection;
            HeadlessEditorWindow.Close(editor);
            if (zone == null) { return; } // Destroyed by the test (deleted-zone case)
            Undo.ClearUndo(zone);
            foreach (ZoneNode node in zone.GetAllNodes()) { Undo.ClearUndo(node); Object.DestroyImmediate(node); }
            Object.DestroyImmediate(zone);
        }
        #endregion

        #region Tests
        [Test]
        public void CreateGUI_BuildsExpectedStructure()
        {
            editor.CreateGUI();

            Assert.IsNotNull(editor.headerLabel);
            Assert.IsNotNull(editor.noZoneMessage);
            Assert.IsNotNull(editor.addGroupButton);
            Assert.IsNotNull(editor.zoneGraphView);
            Assert.IsTrue(editor.rootVisualElement.Contains(editor.headerLabel));
            Assert.IsTrue(editor.rootVisualElement.Contains(editor.zoneGraphView));
        }

        [Test]
        public void CreateGUI_NoZoneSelected_ShowsNoZoneMessageAndHidesGraphView()
        {
            editor.CreateGUI();

            Assert.AreEqual(DisplayStyle.Flex, editor.noZoneMessage.style.display.value);
            Assert.AreEqual(DisplayStyle.None, editor.headerLabel.style.display.value);
            Assert.AreEqual(DisplayStyle.None, editor.zoneGraphView.style.display.value);
            Assert.IsFalse(editor.addGroupButton.enabledSelf);
        }

        [Test]
        public void RefreshFromSelection_WithZoneSelected_ShowsGraphViewAndSetsHeaderAndZone()
        {
            editor.CreateGUI();
            editor.selectedZone = zone;

            editor.RefreshFromSelection();

            Assert.AreEqual(DisplayStyle.None, editor.noZoneMessage.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, editor.headerLabel.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, editor.zoneGraphView.style.display.value);
            Assert.IsTrue(editor.addGroupButton.enabledSelf);
            Assert.AreEqual("MyZone", editor.headerLabel.text);
            Assert.AreSame(zone, editor.zoneGraphView.zone);
        }

        [Test]
        public void RefreshFromSelection_BeforeCreateGUI_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => editor.RefreshFromSelection());
        }

        [UnityTest]
        public IEnumerator AddGroupButtonClick_CallsBeginPlacingGroupOnGraphView()
        {
            editor.CreateGUI();
            editor.selectedZone = zone;
            editor.RefreshFromSelection();
            yield return HeadlessEditorWindow.WaitUntilSettled(editor);

            HeadlessEditorWindow.SendClick(editor.addGroupButton);
            yield return null;

            Assert.IsTrue(editor.zoneGraphView.isPlacingGroup);
        }

        [Test]
        public void OnSelectionChanged_ZoneSelected_UpdatesSelectedZoneAndRefreshes()
        {
            editor.CreateGUI();
            Selection.activeObject = zone;

            editor.OnSelectionChanged();

            Assert.AreSame(zone, editor.selectedZone);
            Assert.AreEqual("MyZone", editor.headerLabel.text);
        }

        [Test]
        public void OnSelectionChanged_NewZoneSelected_CreatesRootNodeAndShowsIt()
        {
            editor.CreateGUI();
            Selection.activeObject = zone;

            editor.OnSelectionChanged();

            Assert.AreEqual(1, zone.GetAllNodes().Count());
            Assert.AreEqual(1, editor.zoneGraphView.nodeLayer.childCount);
        }

        [Test]
        public void OnSelectionChanged_SameZoneReselected_KeepsNodeViews()
        {
            editor.CreateGUI();
            Selection.activeObject = zone;
            editor.OnSelectionChanged();
            VisualElement rootNodeView = editor.zoneGraphView.nodeLayer[0];

            editor.OnSelectionChanged();

            Assert.AreSame(rootNodeView, editor.zoneGraphView.nodeLayer[0]);
        }

        [Test]
        public void OnProjectChanged_SelectedZoneDeleted_ClearsSelectionAndNodeViews()
        {
            editor.CreateGUI();
            Selection.activeObject = zone;
            editor.OnSelectionChanged();
            foreach (ZoneNode node in zone.GetAllNodes()) { Object.DestroyImmediate(node); }
            Object.DestroyImmediate(zone); // Stands in for deleting the asset ~ the window still holds the reference

            editor.OnProjectChanged();

            Assert.IsTrue(ReferenceEquals(editor.selectedZone, null));
            Assert.AreEqual(0, editor.zoneGraphView.nodeLayer.childCount);
            Assert.AreEqual(DisplayStyle.Flex, editor.noZoneMessage.style.display.value);
        }

        [Test]
        public void Undo_NodeCreatedThroughGraph_RebuildsNodeViews()
        {
            editor.CreateGUI();
            Selection.activeObject = zone;
            editor.OnSelectionChanged();
            Undo.IncrementCurrentGroup();
            editor.zoneGraphView.RequestCreateChild(zone.GetRootNode());
            Undo.IncrementCurrentGroup();
            Assert.AreEqual(2, editor.zoneGraphView.nodeLayer.childCount);

            Undo.PerformUndo();

            Assert.AreEqual(1, zone.GetAllNodes().Count());
            Assert.AreEqual(1, editor.zoneGraphView.nodeLayer.childCount);
        }

        [Test]
        public void Undo_UnrelatedObject_KeepsNodeViews()
        {
            editor.CreateGUI();
            Selection.activeObject = zone;
            editor.OnSelectionChanged();
            VisualElement rootNodeView = editor.zoneGraphView.nodeLayer[0];
            var unrelatedAsset = ScriptableObject.CreateInstance<ZoneViewData>();
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(unrelatedAsset, "ZoneEditorTests Unrelated Edit");
            unrelatedAsset.name = "Edited";
            EditorUtility.SetDirty(unrelatedAsset);
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();

            Assert.AreSame(rootNodeView, editor.zoneGraphView.nodeLayer[0], "views not rebuilt");
            Undo.ClearUndo(unrelatedAsset);
            Object.DestroyImmediate(unrelatedAsset);
        }

        [Test]
        public void OnUndoRedoPerformed_SelectedZoneDeleted_ClearsSelectionAndNodeViews()
        {
            editor.CreateGUI();
            Selection.activeObject = zone;
            editor.OnSelectionChanged();
            foreach (ZoneNode node in zone.GetAllNodes()) { Object.DestroyImmediate(node); }
            Object.DestroyImmediate(zone);

            editor.OnUndoRedoPerformed();

            Assert.IsTrue(ReferenceEquals(editor.selectedZone, null));
            Assert.AreEqual(0, editor.zoneGraphView.nodeLayer.childCount);
        }

        [Test]
        public void OnProjectChanged_SelectedZoneStillExists_KeepsSelection()
        {
            editor.CreateGUI();
            Selection.activeObject = zone;
            editor.OnSelectionChanged();

            editor.OnProjectChanged();

            Assert.AreSame(zone, editor.selectedZone);
            Assert.AreEqual(1, editor.zoneGraphView.nodeLayer.childCount);
        }

        [Test]
        public void OnSelectionChanged_ZoneNodeSelected_ResolvesSelectedZoneViaNodesOwner()
        {
            editor.CreateGUI();

            var zoneNode = ScriptableObject.CreateInstance<ZoneNode>();
            zoneNode.preventLocalizationForTests = true;
            zoneNode.SetZoneName("MyZone");

            Dictionary<string, Zone> originalZoneLookupCache = Zone.zoneLookupCache;
            Dictionary<string, Zone> originalSceneReferenceCache = Zone.sceneReferenceCache;
            Zone.zoneLookupCache = new Dictionary<string, Zone> { { "MyZone", zone } };
            Zone.sceneReferenceCache = new Dictionary<string, Zone> { { "MyZone", zone } };

            Selection.activeObject = zoneNode;
            editor.OnSelectionChanged();

            Assert.AreSame(zone, editor.selectedZone);

            Zone.zoneLookupCache = originalZoneLookupCache;
            Zone.sceneReferenceCache = originalSceneReferenceCache;
            Object.DestroyImmediate(zoneNode);
        }

        [Test]
        public void OnSelectionChanged_SomethingElseSelected_LeavesSelectedZoneUnchanged()
        {
            editor.CreateGUI();
            editor.selectedZone = zone;
            var unrelatedAsset = ScriptableObject.CreateInstance<ZoneViewData>();

            Selection.activeObject = unrelatedAsset;
            editor.OnSelectionChanged();

            Assert.AreSame(zone, editor.selectedZone);

            Object.DestroyImmediate(unrelatedAsset);
        }
        #endregion
    }
}
