using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;
using Frankie.Combat.Editor;

namespace Frankie.Tests.Editor
{
    public class SkillReferenceDrawerTests
    {
        // Tested:
        //  - SkillReferenceDrawer - a skill-name string shown as a Skill object field (single field + array elements)
        //  - Picking a skill writes its name;  clearing writes empty;  a skill the name lookup can't resolve (e.g. not addressable) is refused w/ a warning
        //  - External edits (e.g. undo, the Skill Set Editor) - field follows the stored name
        //  - List element removed - remaining fields re-target their (shifted) entries, edits land on the right one
        // Not Tested:
        //  - Object picker window - standard ObjectField behaviour

        // Data Structures
        private class Host : ScriptableObject
        {
            [SkillReference] public string skillReference;
            [SkillReference] public List<string> skillReferences = new();
        }

        // State
        private readonly TestObjectTracker tracker = new();
        private Host host;
        private SerializedObject serializedObject;
        private EditorWindow window;
        private Skill[] projectSkills;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            projectSkills = CombatTestBuilders.LoadAddressableSkills(3);
            host = tracker.Create<Host>("SkillReferenceDrawerTests_Host");
            host.skillReference = projectSkills[0].name;
            host.skillReferences = new List<string> { projectSkills[0].name, projectSkills[1].name, projectSkills[2].name };
            serializedObject = new SerializedObject(host);
            serializedObject.FindProperty(nameof(Host.skillReferences)).isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.Close(window);
            serializedObject.Dispose();
            Undo.ClearUndo(host);
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private IEnumerator BuildInspector()
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow(500f, 400f);
            window.rootVisualElement.Add(new InspectorElement(serializedObject));
            yield return TestWait.Until(() => GetSkillFields().Count == 4);
            Assert.AreEqual(4, GetSkillFields().Count, "Drawer never built (single field + three list elements)");
            yield return HeadlessEditorWindow.Settle();
        }

        private List<ObjectField> GetSkillFields() => window.rootVisualElement.Query<ObjectField>(className: SkillReferenceDrawer.skillFieldClass).ToList(); // Single field first, then array elements
        private ObjectField GetSingleField() => GetSkillFields()[0];
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator StoredNames_ShownAsSkills()
        {
            yield return BuildInspector();

            CollectionAssert.AreEqual(new[] { projectSkills[0], projectSkills[0], projectSkills[1], projectSkills[2] }, GetSkillFields().ConvertAll(field => field.value));
        }

        [UnityTest]
        public IEnumerator PickedSkill_WritesItsName()
        {
            yield return BuildInspector();

            GetSingleField().value = projectSkills[1];

            Assert.AreEqual(projectSkills[1].name, host.skillReference);
        }

        [UnityTest]
        public IEnumerator Cleared_WritesEmpty()
        {
            yield return BuildInspector();

            GetSingleField().value = null;

            Assert.AreEqual(string.Empty, host.skillReference);
        }

        [UnityTest]
        public IEnumerator UnresolvableSkill_RefusedWithWarning()
        {
            Skill unaddressable = tracker.CreateSkill("SkillReferenceDrawerTests_NotAddressable");
            yield return BuildInspector();
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(unaddressable.name)));

            GetSingleField().value = unaddressable;

            Assert.AreEqual(projectSkills[0].name, host.skillReference);
            Assert.AreEqual(projectSkills[0], GetSingleField().value);
        }

        [UnityTest]
        public IEnumerator ExternalEdit_FieldFollowsStoredName()
        {
            yield return BuildInspector();

            serializedObject.FindProperty(nameof(Host.skillReference)).stringValue = projectSkills[1].name;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return TestWait.Until(() => GetSingleField().value == projectSkills[1]);

            Assert.AreEqual(projectSkills[1], GetSingleField().value);
        }

        [UnityTest]
        public IEnumerator ListElementRemoved_RemainingFieldsEditShiftedEntries()
        {
            yield return BuildInspector();

            serializedObject.FindProperty(nameof(Host.skillReferences)).DeleteArrayElementAtIndex(0);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return TestWait.Until(() => GetSkillFields().Count == 3);
            yield return HeadlessEditorWindow.Settle();
            List<ObjectField> listFields = GetSkillFields().GetRange(1, 2);
            CollectionAssert.AreEqual(new[] { projectSkills[1], projectSkills[2] }, listFields.ConvertAll(field => field.value), "fields show the shifted entries");

            listFields[0].value = projectSkills[0]; // Was data[1], now data[0]

            CollectionAssert.AreEqual(new[] { projectSkills[0].name, projectSkills[2].name }, host.skillReferences);
        }
        #endregion
    }
}
