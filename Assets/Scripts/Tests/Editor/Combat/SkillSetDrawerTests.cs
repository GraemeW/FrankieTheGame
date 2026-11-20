using System.Collections;
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
    public class SkillSetDrawerTests
    {
        // Tested:
        //  - SkillSetDrawer - object field bound to the SkillSet, "Open" button shown for a SkillTree or SkillPool (hidden when empty), follows value changes
        // Not Tested:
        //  - Clicking "Open" - opens (docks) the Skill Set Editor window, so checked manually

        // State
        private readonly TestObjectTracker tracker = new();
        private Host host;
        private SerializedObject serializedObject;
        private EditorWindow window;
        private SkillTree skillTree;
        private SkillPool skillPool;

        // Data Structures
        private class Host : ScriptableObject
        {
            public SkillSet skillSet;
        }
        
        #region Setup
        [SetUp]
        public void SetUp()
        {
            host = tracker.Create<Host>("SkillSetDrawerTests_Host");
            skillTree = tracker.CreateSkillTree("SkillSetDrawerTests_Tree", out _);
            skillPool = tracker.CreateSkillPool("SkillSetDrawerTests_Pool");
            serializedObject = new SerializedObject(host);
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.Close(window);
            serializedObject.Dispose();
            skillTree.ClearUndo();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private void SetSkillSet(SkillSet skillSet)
        {
            serializedObject.FindProperty(nameof(Host.skillSet)).objectReferenceValue = skillSet;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private IEnumerator BuildInspector()
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow(500f, 300f);
            window.rootVisualElement.Add(new InspectorElement(serializedObject));
            yield return TestWait.Until(() => GetOpenButton() != null);
            Assert.IsNotNull(GetOpenButton(), "Drawer never built");
            yield return HeadlessEditorWindow.Settle();
        }

        private ObjectField GetSkillSetField() => window.rootVisualElement.Query<ObjectField>().Where(field => field.bindingPath == nameof(Host.skillSet)).First(); // Not the inspector's script field
        private Button GetOpenButton() => window.rootVisualElement.Q<Button>(className: SkillSetDrawer.openButtonClass);
        private bool IsOpenShown() => GetOpenButton().style.display != DisplayStyle.None;
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator SkillTree_ShowsOpenButton()
        {
            SetSkillSet(skillTree);

            yield return BuildInspector();

            Assert.IsTrue(IsOpenShown());
            Assert.AreEqual(skillTree, GetSkillSetField().value);
        }

        [UnityTest]
        public IEnumerator SkillPool_ShowsOpenButton()
        {
            SetSkillSet(skillPool);

            yield return BuildInspector();

            Assert.IsTrue(IsOpenShown());
        }

        [UnityTest]
        public IEnumerator TreeCleared_HidesOpenButton()
        {
            SetSkillSet(skillTree);
            yield return BuildInspector();
            Assert.IsTrue(IsOpenShown(), "precondition");

            SetSkillSet(null);
            yield return TestWait.Until(() => !IsOpenShown());

            Assert.IsFalse(IsOpenShown());
            Assert.IsNull(GetSkillSetField().value);
        }

        [UnityTest]
        public IEnumerator ValueChangedFromEmpty_ShowsOpenButton()
        {
            SetSkillSet(null);
            yield return BuildInspector();

            SetSkillSet(skillTree);
            yield return TestWait.Until(IsOpenShown);

            Assert.IsTrue(IsOpenShown());
        }
        #endregion
    }
}
