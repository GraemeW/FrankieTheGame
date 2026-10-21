using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectDrawerTests
    {
        // Notes:
        //  - Drawers only run through a real InspectorElement (a bare PropertyField never calls CreatePropertyGUI)
        //  - Bound values + TrackPropertyValue callbacks land over the next editor updates ~ tests wait on the expected state (w/ a timeout) rather than a fixed frame count

        // Data Structures
        private sealed class DrawerTarget : ScriptableObject
        {
            public BattleEffectFloatRange floatRange = new(1f, 2f);
            public BattleEffectVector2Range vectorRange = new(Vector2.zero, Vector2.one);
            public BattleEffectTint tint = new();
            public BattleEffectColourCycle colourCycle = new();
            [SerializeReference] public BattleEffectShape shape = new BattleEffectCircle();
        }

        // State
        private HeadlessEditorWindow window;
        private DrawerTarget target;
        private SerializedObject serializedObject;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            window = new HeadlessEditorWindow();
            target = ScriptableObject.CreateInstance<DrawerTarget>();
            serializedObject = new SerializedObject(target);
            window.root.Add(new InspectorElement(serializedObject));
        }

        [TearDown]
        public void TearDown()
        {
            window.Close();
            Object.DestroyImmediate(target);
            window = null;
        }
        #endregion

        #region PrivateMethods
        private PropertyField FindPropertyField(string propertyPath) => window.root.Query<PropertyField>().Where(field => field.bindingPath == propertyPath).First();
        private PropertyField FindTintField(string fieldName) => FindPropertyField($"{nameof(DrawerTarget.tint)}.{fieldName}");
        private PropertyField FindColourCycleField(string fieldName) => FindPropertyField($"{nameof(DrawerTarget.colourCycle)}.{fieldName}");
        private static bool IsShown(VisualElement element) => element.style.display != DisplayStyle.None;
        #endregion
        
        #region RangeDrawer
        [UnityTest]
        public IEnumerator RangeDrawer_FloatRange_IsOneRowBoundToStartAndEnd()
        {
            FloatField startField = window.root.Query<FloatField>().Where(field => field.label == "Float Range").First();
            Assert.IsNotNull(startField, "aligned start field labelled w/ the property name");
            FloatField endField = startField.Query<FloatField>().Where(field => field != startField).First();
            Assert.IsNotNull(endField, "end field sits inside the start field");
            Assert.IsNotNull(startField.Query<Label>().Where(label => label.text == "→").First());

            yield return HeadlessEditorWindow.WaitUntil(() => Mathf.Approximately(endField.value, 2f));
            Assert.AreEqual(1f, startField.value);
            Assert.AreEqual(2f, endField.value);
        }

        [UnityTest]
        public IEnumerator RangeDrawer_EditingTheEndField_WritesTheProperty()
        {
            FloatField startField = window.root.Query<FloatField>().Where(field => field.label == "Float Range").First();
            FloatField endField = startField.Query<FloatField>().Where(field => field != startField).First();
            yield return HeadlessEditorWindow.WaitUntil(() => Mathf.Approximately(endField.value, 2f));

            endField.value = 5f;
            yield return HeadlessEditorWindow.WaitUntil(() => Mathf.Approximately(target.floatRange.GetEnd(), 5f));

            Assert.AreEqual(5f, target.floatRange.GetEnd());
            Assert.AreEqual(1f, target.floatRange.GetStart());
        }

        [UnityTest]
        public IEnumerator RangeDrawer_Vector2Range_IsOneRowBoundToStartAndEnd()
        {
            Vector2Field startField = window.root.Query<Vector2Field>().Where(field => field.label == "Vector Range").First();
            Assert.IsNotNull(startField);
            Vector2Field endField = startField.Query<Vector2Field>().Where(field => field != startField).First();
            Assert.IsNotNull(endField);

            yield return HeadlessEditorWindow.WaitUntil(() => endField.value == Vector2.one);
            Assert.AreEqual(Vector2.zero, startField.value);
            Assert.AreEqual(Vector2.one, endField.value);
        }
        #endregion

        #region TintDrawer
        [Test]
        public void TintDrawer_IsAFoldoutNamedForTheProperty()
        {
            Assert.IsNotNull(window.root.Query<Foldout>().Where(foldout => foldout.text == "Tint").First());
        }

        [TestCase(BattleEffectTintMode.Solid, false, false, false, false)]
        [TestCase(BattleEffectTintMode.Linear, true, true, true, false)]
        [TestCase(BattleEffectTintMode.Wave, true, true, true, true)]
        [TestCase(BattleEffectTintMode.Radial, true, false, true, true)]
        public void TintDrawer_ShowsOnlyTheModesFields(BattleEffectTintMode mode, bool isColourBShown, bool isAngleShown, bool isFrequencyShown, bool isSpeedShown)
        {
            // Rebuilt per mode (the drawer reads the mode when it's created)
            serializedObject.FindProperty($"{nameof(DrawerTarget.tint)}.{BattleEffectTint.modeField}").intValue = (int)mode;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            window.root.Clear();
            window.root.Add(new InspectorElement(serializedObject));

            Assert.AreEqual(isColourBShown, IsShown(FindTintField(BattleEffectTint.colourBField)), "colour B");
            Assert.AreEqual(isAngleShown, IsShown(FindTintField(BattleEffectTint.angleDegreesField)), "angle");
            Assert.AreEqual(isFrequencyShown, IsShown(FindTintField(BattleEffectTint.bandsPerEUField)), "frequency");
            Assert.AreEqual(isSpeedShown, IsShown(FindTintField(BattleEffectTint.scrollRateField)), "speed");
        }

        [UnityTest]
        public IEnumerator TintDrawer_ModeChange_RefreshesTheShownFields()
        {
            PropertyField speedField = FindTintField(BattleEffectTint.scrollRateField);
            Assert.IsFalse(IsShown(speedField), "solid hides speed");

            serializedObject.FindProperty($"{nameof(DrawerTarget.tint)}.{BattleEffectTint.modeField}").intValue = (int)BattleEffectTintMode.Wave;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(speedField));

            Assert.IsTrue(IsShown(speedField));
        }
        #endregion

        #region ColourCycleDrawer
        [Test]
        public void ColourCycleDrawer_IsAFoldoutNamedForTheProperty()
        {
            Assert.IsNotNull(window.root.Query<Foldout>().Where(foldout => foldout.text == "Colour Cycle").First());
        }

        [TestCase(0, false, false, false)]
        [TestCase(1, true, false, false)]
        [TestCase(3, true, true, true)]
        public void ColourCycleDrawer_ShowsOnlyTheCountsColours(int colourCount, bool isColourBShown, bool isColourCShown, bool isColourDShown)
        {
            // Rebuilt per count (the drawer reads the count when it's created)
            serializedObject.FindProperty($"{nameof(DrawerTarget.colourCycle)}.{BattleEffectColourCycle.colourCountField}").intValue = colourCount;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            window.root.Clear();
            window.root.Add(new InspectorElement(serializedObject));

            Assert.AreEqual(isColourBShown, IsShown(FindColourCycleField(BattleEffectColourCycle.colourBField)), "colour B");
            Assert.AreEqual(isColourCShown, IsShown(FindColourCycleField(BattleEffectColourCycle.colourCField)), "colour C");
            Assert.AreEqual(isColourDShown, IsShown(FindColourCycleField(BattleEffectColourCycle.colourDField)), "colour D");
        }

        [UnityTest]
        public IEnumerator ColourCycleDrawer_CountChange_RefreshesTheShownColours()
        {
            PropertyField colourBField = FindColourCycleField(BattleEffectColourCycle.colourBField);
            Assert.IsFalse(IsShown(colourBField), "a count of 0 hides colour B");

            serializedObject.FindProperty($"{nameof(DrawerTarget.colourCycle)}.{BattleEffectColourCycle.colourCountField}").intValue = 1;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(colourBField));

            Assert.IsTrue(IsShown(colourBField));
        }
        #endregion

        #region ShapeDrawer
        [Test]
        public void ShapeDrawer_ListsEveryShapeAndShowsTheCurrentOne()
        {
            DropdownField typeField = window.root.Query<DropdownField>().Where(field => field.label == "Shape").First();

            Assert.IsNotNull(typeField);
            CollectionAssert.IsSubsetOf(new[] { "Chevron", "Circle", "Diamond", "Segment", "Squiggle" }, typeField.choices);
            Assert.AreEqual(TypeCache.GetTypesDerivedFrom<BattleEffectShape>().Count(type => !type.IsAbstract), typeField.choices.Count);
            Assert.AreEqual("Circle", typeField.value);
        }

        [UnityTest]
        public IEnumerator ShapeDrawer_PickingAType_SwapsTheShapeAndItsSettings()
        {
            DropdownField typeField = window.root.Query<DropdownField>().Where(field => field.label == "Shape").First();

            typeField.value = "Diamond";
            yield return HeadlessEditorWindow.WaitUntil(() => FindPropertyField("shape.aspectRatio") != null);

            Assert.IsInstanceOf<BattleEffectDiamond>(target.shape);
            Assert.IsNotNull(FindPropertyField("shape.aspectRatio"), "diamond settings shown");
        }

        [UnityTest]
        public IEnumerator ShapeDrawer_ShapeSwappedOutsideTheDrawer_UpdatesTheDropdown()
        {
            // e.g. undo / redo
            DropdownField typeField = window.root.Query<DropdownField>().Where(field => field.label == "Shape").First();

            serializedObject.FindProperty(nameof(DrawerTarget.shape)).managedReferenceValue = new BattleEffectSquiggle();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => typeField.value == "Squiggle");

            Assert.AreEqual("Squiggle", typeField.value);
            Assert.IsNotNull(FindPropertyField("shape.wavelengthSU"), "squiggle settings shown");
        }
        #endregion
    }
}
