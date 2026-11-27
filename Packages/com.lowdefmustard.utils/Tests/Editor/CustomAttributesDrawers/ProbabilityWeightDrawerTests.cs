using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ProbabilityWeightDrawerTests
    {
        // Tested:
        //  - ProbabilityWeightField.GetShareText - share of the total over landable entries, zero weight flagged as never picked (left out of the total), empty value (null / destroyed, read through the pair interface) keeps its share (or is never picked w/ isEmptyValueSkipped), not a pair (weight only), not-in-a-list
        //  - Drawer UI - share label per element, refreshes when a sibling's weight changes, clamps edits below min, error box on a non-int field
        // Not Tested:
        //  - Prefab override bar / context menu - comes from binding the IntegerField, not this drawer

        // State
        private readonly TestObjectTracker tracker = new();
        private Host host;
        private ValueAsset valueAsset;
        private SerializedObject serializedObject;
        private EditorWindow window;

        // Data Structures
        [Serializable]
        private class Pair : IObjectProbabilityPair<Object>
        {
            public Object value;
            [ProbabilityWeight(min: 1)] public int weight = 1;

            Object IObjectProbabilityPair<Object>.value => value;
            int IObjectProbabilityPair<Object>.weight => weight;
        }

        private class ValueAsset : ScriptableObject { }
        [Serializable] private class NoValuePair { [ProbabilityWeight] public int weight; }

        private class Host : ScriptableObject
        {
            public List<Pair> pairs = new();
            public List<NoValuePair> noValuePairs = new();
            [ProbabilityWeight] public int notInList;
            [ProbabilityWeight] public string notAnInt;
        }
        
        #region Setup
        [SetUp]
        public void SetUp()
        {
            host = tracker.Create<Host>("ProbabilityWeightDrawerTests_Host");
            valueAsset = tracker.Create<ValueAsset>("ProbabilityWeightDrawerTests_ValueAsset");
            serializedObject = new SerializedObject(host);
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.Close(window);
            serializedObject?.Dispose();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private static string PairWeightPath(int index) => $"{nameof(Host.pairs)}.Array.data[{index}].{nameof(Pair.weight)}";

        private void SetPairs(params (bool hasValue, int weight)[] entries)
        {
            host.pairs.Clear();
            foreach ((bool hasValue, int weight) in entries) { host.pairs.Add(new Pair { value = hasValue ? valueAsset : null, weight = weight }); }
            serializedObject.Update();
        }

        private string ShareText(int index, out bool isExcluded, out bool isEmpty, out string tooltip, bool isEmptyValueSkipped = false) => ProbabilityWeightField.GetShareText(serializedObject, PairWeightPath(index), isEmptyValueSkipped, out isExcluded, out isEmpty, out tooltip);
        private string ShareText(int index, bool isEmptyValueSkipped = false) => ShareText(index, out _, out _, out _, isEmptyValueSkipped);

        private IEnumerator BuildInspector()
        {
            SerializedProperty pairsProperty = serializedObject.FindProperty(nameof(Host.pairs));
            pairsProperty.isExpanded = true;
            for (int i = 0; i < pairsProperty.arraySize; i++) { pairsProperty.GetArrayElementAtIndex(i).isExpanded = true; }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            window = HeadlessEditorWindow.CreateOffscreenWindow(500f, 600f);
            window.rootVisualElement.Add(new InspectorElement(serializedObject));
            yield return TestWait.Until(() => ShareLabels().Count == pairsProperty.arraySize);
            Assert.AreEqual(pairsProperty.arraySize, ShareLabels().Count, "Share labels never built");
            yield return HeadlessEditorWindow.Settle();
        }
        
        private List<Label> ShareLabels()
        {
            // Pairs list only ~ the host's non-list weight field has a (blank) share label too
            ListView pairsList = window.rootVisualElement.Query<ListView>().Where(listView => listView.bindingPath == nameof(Host.pairs)).First();
            return pairsList != null ? pairsList.Query<Label>(className: ProbabilityWeightField.shareLabelClass).ToList() : new List<Label>();
        }
        #endregion

        #region ShareTests
        [Test]
        public void GetShareText_PickedEntries_ShareOfTotal()
        {
            SetPairs((true, 1), (true, 3));

            Assert.AreEqual("25%", ShareText(0));
            Assert.AreEqual("75%", ShareText(1));
        }

        [Test]
        public void GetShareText_NonWholeShare_OneDecimal()
        {
            SetPairs((true, 1), (true, 1), (true, 1));

            Assert.AreEqual("33.3%", ShareText(0));
        }

        [Test]
        public void GetShareText_EmptyValue_KeepsItsShare()
        {
            // e.g. a loot table's "no loot this time" entry ~ rolls land on it and pick nothing
            SetPairs((true, 1), (false, 3));

            Assert.AreEqual("25%", ShareText(0));
            Assert.AreEqual("75%", ShareText(1, out bool isExcluded, out bool isEmpty, out string tooltip));
            Assert.IsFalse(isExcluded);
            Assert.IsTrue(isEmpty);
            StringAssert.Contains("No Object", tooltip);
        }

        [Test]
        public void GetShareText_DestroyedValue_CountsAsEmpty()
        {
            ValueAsset destroyedAsset = tracker.Create<ValueAsset>("ProbabilityWeightDrawerTests_DestroyedAsset");
            SetPairs((true, 1), (true, 3));
            host.pairs[1].value = destroyedAsset;
            serializedObject.Update();
            Object.DestroyImmediate(destroyedAsset);

            Assert.AreEqual("75%", ShareText(1, out bool isExcluded, out bool isEmpty, out _));
            Assert.IsFalse(isExcluded);
            Assert.IsTrue(isEmpty);
        }

        [Test]
        public void GetShareText_EmptyValueSkipped_NeverPickedAndLeftOutOfTotal()
        {
            // e.g. skills ~ SkillSet filters empties before rolling
            SetPairs((true, 1), (false, 3));

            Assert.AreEqual("100%", ShareText(0, isEmptyValueSkipped: true));
            ShareText(1, out bool isExcluded, out _, out string tooltip, isEmptyValueSkipped: true);
            Assert.IsTrue(isExcluded);
            StringAssert.Contains("No Object", tooltip);
        }

        [Test]
        public void GetShareText_ZeroWeight_NeverPickedAndLeftOutOfTotal()
        {
            SetPairs((true, 0), (true, 2));

            ShareText(0, out bool isExcluded, out _, out _);
            Assert.IsTrue(isExcluded);
            Assert.AreEqual("100%", ShareText(1));
        }

        [Test]
        public void GetShareText_NotAPair_OnlyWeightCounts()
        {
            host.noValuePairs.Add(new NoValuePair { weight = 1 });
            host.noValuePairs.Add(new NoValuePair { weight = 1 });
            serializedObject.Update();

            string text = ProbabilityWeightField.GetShareText(serializedObject, $"{nameof(Host.noValuePairs)}.Array.data[0].{nameof(NoValuePair.weight)}", false, out bool isExcluded, out _, out _);

            Assert.AreEqual("50%", text);
            Assert.IsFalse(isExcluded);
        }

        [Test]
        public void GetShareText_NotInList_Blank()
        {
            Assert.AreEqual(string.Empty, ProbabilityWeightField.GetShareText(serializedObject, nameof(Host.notInList), false, out bool isExcluded, out _, out _));
            Assert.IsFalse(isExcluded);
        }
        #endregion

        #region DrawerTests
        [UnityTest]
        public IEnumerator Drawer_ShowsShareLabelPerElement()
        {
            SetPairs((true, 1), (true, 3));

            yield return BuildInspector();

            List<Label> shareLabels = ShareLabels();
            Assert.AreEqual("25%", shareLabels[0].text);
            Assert.AreEqual("75%", shareLabels[1].text);
        }

        [UnityTest]
        public IEnumerator Drawer_SiblingWeightChanged_RefreshesShare()
        {
            SetPairs((true, 1), (true, 1));
            yield return BuildInspector();

            serializedObject.FindProperty(PairWeightPath(1)).intValue = 3;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return TestWait.Until(() => ShareLabels()[0].text == "25%");

            Assert.AreEqual("25%", ShareLabels()[0].text);
        }

        [UnityTest]
        public IEnumerator Drawer_EditBelowMin_ClampsToMin()
        {
            SetPairs((true, 2));
            yield return BuildInspector();
            var weightField = ShareLabels()[0].parent.Q<IntegerField>();

            weightField.value = -4;
            yield return TestWait.Until(() => host.pairs[0].weight == 1);

            Assert.AreEqual(1, host.pairs[0].weight);
            Assert.AreEqual(1, weightField.value);
        }

        [Test]
        public void Drawer_NonIntField_ShowsErrorBox()
        {
            var drawer = new ProbabilityWeightDrawer();

            VisualElement element = drawer.CreatePropertyGUI(serializedObject.FindProperty(nameof(Host.notAnInt)));

            Assert.IsInstanceOf<HelpBox>(element);
        }
        #endregion
    }
}
