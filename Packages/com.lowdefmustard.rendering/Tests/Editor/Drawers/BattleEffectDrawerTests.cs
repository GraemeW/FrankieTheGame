using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Rendering.Editor;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectDrawerTests
    {
        // Notes:
        //  - Bound values + TrackPropertyValue callbacks land over the next editor updates ~ so tests wait on the expected state
        //  - The drawers + sections track only their own property (tests verify that parent tracks on child edits)
        //      - this does NOT apply to [SerializeReference] parents (e.g. a shape)
        //      - the shape drawer + the recipe inspector's item / instance headers track the whole object instead

        // Data Structures
        private sealed class DrawerTarget : ScriptableObject
        {
            public BattleEffectFloatRange floatRange = new(1f, 2f);
            public BattleEffectFloatRange constantRange = new(3f);
            public BattleEffectVector2Range halfChangingRange = new(Vector2.zero, new Vector2(0f, 1f));
            public BattleEffectVector2Range vectorRange = new(Vector2.zero, Vector2.one);
            public BattleEffectTint tint = new();
            public BattleEffectAltColours altColours = new();
            public BattleEffectColourCycle colourCycle = new();
            public BattleEffectBeats beats = new();
            public BattleEffectStripes stripes = new();
            public BattleEffectClip clip = new();
            public BattleEffectOutlineEdgeGradient outlineEdgeGradient = new();
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
        private static bool IsShown(VisualElement element) => element.style.display != DisplayStyle.None;
        private BattleEffectSection FindSection(string title) => window.root.Query<BattleEffectSection>().Where(section => section.text == title).First();
        private PropertyField FindTintField(string fieldName) => FindPropertyField($"{nameof(DrawerTarget.tint)}.{fieldName}");
        private PropertyField FindColourCycleField(string fieldName) => FindPropertyField($"{nameof(DrawerTarget.colourCycle)}.{fieldName}");
        private PropertyField FindAltColoursField(string fieldName) => FindPropertyField($"{nameof(DrawerTarget.altColours)}.{fieldName}");
        private static StyleColor GetInputColour(VisualElement field, int index) => field.Query(className: TextInputBaseField<float>.inputUssClassName).AtIndex(index).style.backgroundColor;
        private PropertyField FindBeatsField(string fieldName) => FindPropertyField($"{nameof(DrawerTarget.beats)}.{fieldName}");
        #endregion
        
        #region Tracking
        [UnityTest]
        public IEnumerator TrackPropertyValue_Parent_FiresOnAChildEdit([Values(nameof(DrawerTarget.altColours), nameof(DrawerTarget.floatRange))] string parentName)
        {
            bool isFired = false;
            var tracker = new VisualElement();
            window.root.Add(tracker);
            tracker.TrackPropertyValue(serializedObject.FindProperty(parentName), _ => isFired = true);
            yield return null;
            yield return null;
            isFired = false;

            if (parentName == nameof(DrawerTarget.altColours)) { serializedObject.FindProperty($"{parentName}.{BattleEffectAltColours.countField}").intValue = 2; }
            else { serializedObject.FindProperty($"{parentName}.{BattleEffectFloatRange.endField}").floatValue = 7f; }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => isFired);

            Assert.IsTrue(isFired);
        }

        [UnityTest]
        public IEnumerator TrackPropertyValue_Parent_IgnoresASiblingsEdit()
        {
            bool isFired = false;
            var tracker = new VisualElement();
            window.root.Add(tracker);
            tracker.TrackPropertyValue(serializedObject.FindProperty(nameof(DrawerTarget.altColours)), _ => isFired = true);
            yield return null;

            serializedObject.FindProperty($"{nameof(DrawerTarget.beats)}.{BattleEffectBeats.countField}").intValue = 2;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => isFired, 0.5f);

            Assert.IsFalse(isFired);
        }
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
        
        [Test]
        public void RangeDrawer_TintsOnlyTheChangingInputs()
        {
            FloatField changing = window.root.Query<FloatField>().Where(field => field.label == "Float Range").First();
            FloatField constant = window.root.Query<FloatField>().Where(field => field.label == "Constant Range").First();
            Vector2Field half = window.root.Query<Vector2Field>().Where(field => field.label == "Half Changing Range").First();
            StyleColor tint = BattleEffectInspectorColours.changingInput;

            Assert.AreEqual(tint, GetInputColour(changing, 0), "1 -> 2:  start");
            Assert.AreEqual(tint, GetInputColour(changing, 1), "1 -> 2:  end");
            Assert.AreEqual(StyleKeyword.Null, GetInputColour(constant, 0).keyword, "3 -> 3");
            Assert.AreEqual(StyleKeyword.Null, GetInputColour(half, 0).keyword, "x 0 -> 0:  start");
            Assert.AreEqual(tint, GetInputColour(half, 1), "y 0 -> 1:  start");
            Assert.AreEqual(StyleKeyword.Null, GetInputColour(half, 2).keyword, "x 0 -> 0:  end");
            Assert.AreEqual(tint, GetInputColour(half, 3), "y 0 -> 1:  end");
        }

        [UnityTest]
        public IEnumerator RangeDrawer_EndEdit_RefreshesTheTint()
        {
            FloatField constant = window.root.Query<FloatField>().Where(field => field.label == "Constant Range").First();

            serializedObject.FindProperty($"{nameof(DrawerTarget.constantRange)}.{BattleEffectFloatRange.endField}").floatValue = 4f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => GetInputColour(constant, 0).keyword != StyleKeyword.Null);

            Assert.AreEqual((StyleColor)BattleEffectInspectorColours.changingInput, GetInputColour(constant, 0));
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

        #region AltColoursDrawer
        [Test]
        public void AltColoursDrawer_IsAFoldoutNamedForTheProperty()
        {
            Assert.IsNotNull(window.root.Query<Foldout>().Where(foldout => foldout.text == "Alt Colours").First());
        }

        [TestCase(0, false, false, false)]
        [TestCase(1, true, false, false)]
        [TestCase(3, true, true, true)]
        public void AltColoursDrawer_ShowsOnlyTheCountsColours(int count, bool isColourBShown, bool isColourCShown, bool isColourDShown)
        {
            // Rebuilt per count (the drawer reads the count when it's created)
            serializedObject.FindProperty($"{nameof(DrawerTarget.altColours)}.{BattleEffectAltColours.countField}").intValue = count;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            window.root.Clear();
            window.root.Add(new InspectorElement(serializedObject));

            Assert.AreEqual(isColourBShown, IsShown(FindAltColoursField(BattleEffectAltColours.colourBField)), "colour B");
            Assert.AreEqual(isColourCShown, IsShown(FindAltColoursField(BattleEffectAltColours.colourCField)), "colour C");
            Assert.AreEqual(isColourDShown, IsShown(FindAltColoursField(BattleEffectAltColours.colourDField)), "colour D");
        }

        [UnityTest]
        public IEnumerator AltColoursDrawer_CountChange_RefreshesTheShownColours()
        {
            PropertyField colourBField = FindAltColoursField(BattleEffectAltColours.colourBField);
            Assert.IsFalse(IsShown(colourBField), "a count of 0 hides colour B");

            serializedObject.FindProperty($"{nameof(DrawerTarget.altColours)}.{BattleEffectAltColours.countField}").intValue = 1;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(colourBField));

            Assert.IsTrue(IsShown(colourBField));
        }
        #endregion

        #region ColourCycleDrawer
        [Test]
        public void ColourCycleDrawer_IsAFoldoutNamedForTheProperty()
        {
            Assert.IsNotNull(window.root.Query<Foldout>().Where(foldout => foldout.text == "Colour Cycle").First());
        }

        [UnityTest]
        public IEnumerator ColourCycleDrawer_CopySource_HidesTheRate()
        {
            PropertyField rateField = FindColourCycleField(BattleEffectColourCycle.rateField);
            Assert.IsTrue(IsShown(rateField), "time source shows the rate");

            serializedObject.FindProperty($"{nameof(DrawerTarget.colourCycle)}.{BattleEffectColourCycle.sourceField}").enumValueIndex = (int)BattleEffectColourSource.Copy;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => !IsShown(rateField));

            Assert.IsFalse(IsShown(rateField));
        }

        [UnityTest]
        public IEnumerator ColourCycleDrawer_PatternSource_ShowsThePatternAndRate()
        {
            PropertyField patternField = FindColourCycleField(BattleEffectColourCycle.patternField);
            Assert.IsFalse(IsShown(patternField), "time source hides the pattern");

            serializedObject.FindProperty($"{nameof(DrawerTarget.colourCycle)}.{BattleEffectColourCycle.sourceField}").enumValueIndex = (int)BattleEffectColourSource.Pattern;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(patternField));

            Assert.IsTrue(IsShown(patternField));
            Assert.IsTrue(IsShown(FindColourCycleField(BattleEffectColourCycle.rateField)), "its steps per second");
        }
        #endregion

        #region BeatsDrawer
        [UnityTest]
        public IEnumerator BeatsDrawer_ShowsOnlyTheCountsBeats()
        {
            PropertyField beatAField = FindBeatsField(BattleEffectBeats.beatAField);
            PropertyField beatCField = FindBeatsField(BattleEffectBeats.beatCField);
            Assert.IsFalse(IsShown(beatAField), "a count of 0 hides every beat");

            serializedObject.FindProperty($"{nameof(DrawerTarget.beats)}.{BattleEffectBeats.countField}").intValue = 2;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(beatAField));

            Assert.IsTrue(IsShown(FindBeatsField(BattleEffectBeats.beatBField)), "beat B");
            Assert.IsFalse(IsShown(beatCField), "beat C");
        }
        #endregion

        #region SectionDrawers
        [Test]
        public void SectionDrawers_Unused_AreFoldedWithTheirSettingsHidden()
        {
            Assert.IsFalse(FindSection("Stripes").value, "stripes (0 per EU) folded");
            Assert.IsFalse(IsShown(FindPropertyField($"{nameof(DrawerTarget.stripes)}.{BattleEffectStripes.dutyFractionField}")));
            Assert.IsFalse(FindSection("Clip").value, "clip (none) folded");
            Assert.IsFalse(IsShown(FindPropertyField($"{nameof(DrawerTarget.clip)}.{BattleEffectClip.halfSizeSUField}")));
            Assert.IsFalse(IsShown(FindPropertyField($"{nameof(DrawerTarget.clip)}.{BattleEffectClip.offsetSUField}")));
            Assert.IsFalse(FindSection("Outline Edge Gradient").value, "edge gradient (off) folded");
            Assert.IsFalse(IsShown(FindPropertyField($"{nameof(DrawerTarget.outlineEdgeGradient)}.{BattleEffectOutlineEdgeGradient.colourField}")));
            Assert.IsFalse(FindSection("Alt Colours").IsEnabled(), "alt colours (count 0)");
        }

        [UnityTest]
        public IEnumerator SectionDrawers_TurningOn_EnablesAndShowsTheSettings()
        {
            PropertyField dutyField = FindPropertyField($"{nameof(DrawerTarget.stripes)}.{BattleEffectStripes.dutyFractionField}");

            serializedObject.FindProperty($"{nameof(DrawerTarget.stripes)}.{BattleEffectStripes.stripesPerEUField}").floatValue = 8f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(dutyField));

            Assert.IsTrue(IsShown(dutyField));
            Assert.IsTrue(FindSection("Stripes").IsEnabled());
        }

        [Test]
        public void SectionDrawers_InUse_StartUnfolded()
        {
            serializedObject.FindProperty($"{nameof(DrawerTarget.clip)}.{BattleEffectClip.shapeField}").intValue = (int)BattleEffectClipShape.Diamond;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            window.root.Clear();
            window.root.Add(new InspectorElement(serializedObject));

            Assert.IsTrue(FindSection("Clip").value);
            Assert.IsTrue(IsShown(FindPropertyField($"{nameof(DrawerTarget.clip)}.{BattleEffectClip.halfSizeSUField}")));
            Assert.IsTrue(IsShown(FindPropertyField($"{nameof(DrawerTarget.clip)}.{BattleEffectClip.offsetSUField}")));
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

        [UnityTest]
        public IEnumerator ShapeDrawer_ShownWhen_ShowsTheSettingOnlyForItsValues()
        {
            serializedObject.FindProperty(nameof(DrawerTarget.shape)).managedReferenceValue = new BattleEffectSquiggle();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => FindPropertyField("shape.plateauFraction") != null);
            PropertyField plateauField = FindPropertyField("shape.plateauFraction");
            Assert.IsFalse(IsShown(plateauField), "sine");

            serializedObject.FindProperty("shape.waveform").intValue = (int)BattleEffectWaveform.Trapezoid;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(plateauField));

            Assert.IsTrue(IsShown(plateauField), "trapezoid");
            Assert.IsFalse(IsShown(FindPropertyField("shape.dutyFraction")), "duty:  pulse only");

            serializedObject.FindProperty("shape.waveform").intValue = (int)BattleEffectWaveform.Pulse;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(FindPropertyField("shape.dutyFraction")));

            Assert.IsTrue(IsShown(plateauField), "pulse plateau");
            Assert.IsTrue(IsShown(FindPropertyField("shape.dutyFraction")), "pulse duty");
        }
        #endregion
    }
}
