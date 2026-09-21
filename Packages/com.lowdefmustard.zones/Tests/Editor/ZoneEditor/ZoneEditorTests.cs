using System.Collections;
using LowDefMustard.Zones.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;

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
            if (editor != null) { editor.Close(); }
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
            yield return null;

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
        public void OnSelectionChanged_ZoneNodeSelected_ResolvesSelectedZoneViaNodesOwner()
        {
            editor.CreateGUI();

            var zoneNode = ScriptableObject.CreateInstance<ZoneNode>();
            zoneNode.preventLocalizationForTests = true;
            LogAssert.ignoreFailingMessages = true; // SetZoneName's localization-bridge lookup
            zoneNode.SetZoneName("MyZone");
            LogAssert.ignoreFailingMessages = false;

            var originalZoneLookupCache = Zone.zoneLookupCache;
            var originalSceneReferenceCache = Zone.sceneReferenceCache;
            Zone.zoneLookupCache = new System.Collections.Generic.Dictionary<string, Zone> { { "MyZone", zone } };
            Zone.sceneReferenceCache = new System.Collections.Generic.Dictionary<string, Zone>();

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
