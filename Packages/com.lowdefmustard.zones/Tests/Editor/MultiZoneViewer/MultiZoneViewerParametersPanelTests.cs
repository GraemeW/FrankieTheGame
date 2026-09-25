using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LowDefMustard.Zones.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.Zones.Tests.Editor
{
    // Covers BuildParametersPanel's field wiring: the three scaling-factor FloatFields shown over the canvas
    public class MultiZoneViewerParametersPanelTests
    {
        // State
        private MultiZoneViewer viewer;
        private bool attached;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            viewer = ScriptableObject.CreateInstance<MultiZoneViewer>();
            attached = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (viewer != null)
            {
                if (attached) { viewer.Close(); }
                else { Object.DestroyImmediate(viewer); }
            }
        }
        #endregion

        #region PrivateMethods
        private List<FloatField> GetScalingFields()
        {
            viewer.CreateGUI();
            return viewer.canvas.Query<FloatField>().ToList();
        }

        private void Attach()
        {
            viewer.ShowUtility();
            viewer.position = new Rect(-10000, -10000, 400, 300);
            attached = true;
        }
        #endregion

        #region Tests
        [Test]
        public void BuildParametersPanel_CreatesThreeFieldsSeededFromCurrentTunables()
        {
            viewer.worldToSnapshotScalingFactor = 80f;
            viewer.snapshotToZoneViewScalingFactor = 0.15f;
            viewer.additionalMaxScalingFactor = 5f;

            List<FloatField> fields = GetScalingFields();

            Assert.AreEqual(3, fields.Count);
            Assert.AreEqual(80f, fields[0].value);
            Assert.AreEqual(0.15f, fields[1].value);
            Assert.AreEqual(5f, fields[2].value);
        }

        [Test]
        public void BuildParametersPanel_LabelsMatchEachTunablesPurpose()
        {
            List<FloatField> fields = GetScalingFields();

            var labels = fields.Select(field => field.parent.Q<Label>().text).ToList();

            Assert.AreEqual("World-to-Snapshot Scaling", labels[0]);
            Assert.AreEqual("Snapshot-to-ZoneView Scaling", labels[1]);
            Assert.AreEqual("Additional Max Scaling", labels[2]);
        }

        [UnityTest]
        public IEnumerator ChangingWorldToSnapshotField_UpdatesTheBackingTunable()
        {
            viewer.worldToSnapshotScalingFactor = 80f;
            List<FloatField> fields = GetScalingFields();
            Attach();
            yield return null;

            fields[0].value = 120f;
            yield return null;

            Assert.AreEqual(120f, viewer.worldToSnapshotScalingFactor);
        }

        [UnityTest]
        public IEnumerator ChangingSnapshotToZoneViewField_UpdatesTheBackingTunable()
        {
            viewer.snapshotToZoneViewScalingFactor = 0.15f;
            List<FloatField> fields = GetScalingFields();
            Attach();
            yield return null;

            fields[1].value = 0.3f;
            yield return null;

            Assert.AreEqual(0.3f, viewer.snapshotToZoneViewScalingFactor);
        }

        [UnityTest]
        public IEnumerator ChangingAdditionalMaxScalingField_UpdatesTheBackingTunable()
        {
            viewer.additionalMaxScalingFactor = 5f;
            List<FloatField> fields = GetScalingFields();
            Attach();
            yield return null;

            fields[2].value = 8f;
            yield return null;

            Assert.AreEqual(8f, viewer.additionalMaxScalingFactor);
        }

        [UnityTest]
        public IEnumerator ChangingOneField_DoesNotAffectTheOtherTwoTunables()
        {
            viewer.worldToSnapshotScalingFactor = 80f;
            viewer.snapshotToZoneViewScalingFactor = 0.15f;
            viewer.additionalMaxScalingFactor = 5f;
            List<FloatField> fields = GetScalingFields();
            Attach();
            yield return null;

            fields[0].value = 999f;
            yield return null;

            Assert.AreEqual(0.15f, viewer.snapshotToZoneViewScalingFactor);
            Assert.AreEqual(5f, viewer.additionalMaxScalingFactor);
        }
        #endregion
    }
}
