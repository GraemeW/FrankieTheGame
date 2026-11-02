using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AssetNameLabelTests
    {
        // Const Tunables
        private const string _tempFolder = "Assets/_TEMP_AssetNameLabelTests_SafeToDelete";
        private const string _assetName = "TestClip";

        // State
        private EditorWindow window;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
            if (AssetDatabase.IsValidFolder(_tempFolder)) { AssetDatabase.DeleteAsset(_tempFolder); }
        }
        #endregion

        #region PrivateMethods
        private static AnimationClip CreateTempAsset(string assetName = _assetName)
        {
            if (!AssetDatabase.IsValidFolder(_tempFolder)) { AssetDatabase.CreateFolder("Assets", "_TEMP_AssetNameLabelTests_SafeToDelete"); }
            var clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, $"{_tempFolder}/{assetName}.anim");
            return clip;
        }

        private AssetNameLabel SetUpAttached(Object asset)
        {
            window = HeadlessEditorWindowTestHelper.CreateOffscreenWindow();
            var nameLabel = new AssetNameLabel(false);
            nameLabel.SetAsset(asset);
            window.rootVisualElement.Add(nameLabel);
            return nameLabel;
        }

        private static Label GetLabel(AssetNameLabel nameLabel) => nameLabel.Q<Label>();
        private static TextField GetField(AssetNameLabel nameLabel) => nameLabel.Q<TextField>();
        private static bool IsEditing(AssetNameLabel nameLabel) => GetField(nameLabel).style.display.value == DisplayStyle.Flex;

        private static void SendClick(VisualElement target, int clickCount)
        {
            using ClickEvent clickEvent = ClickEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, clickCount = clickCount });
            clickEvent.target = target;
            target.SendEvent(clickEvent);
        }

        private static void PressKey(VisualElement target, KeyCode keyCode)
        {
            using KeyDownEvent keyDownEvent = KeyDownEvent.GetPooled('\0', keyCode, EventModifiers.None);
            keyDownEvent.target = target;
            target.SendEvent(keyDownEvent);
        }

        private static void SendFocusOut(TextField field)
        {
            // Stands in for the field losing focus (a headless window doesn't reliably hold keyboard focus)
            using FocusOutEvent focusOutEvent = FocusOutEvent.GetPooled(field, null, FocusChangeDirection.unspecified, field.focusController);
            focusOutEvent.target = field;
            field.SendEvent(focusOutEvent);
        }
        #endregion

        #region Tests
        [Test]
        public void SetAsset_Null_ShowsNoneText()
        {
            var nameLabel = new AssetNameLabel(false);

            nameLabel.SetAsset(null);

            Assert.AreEqual("None", GetLabel(nameLabel).text);
        }

        [Test]
        public void SetAsset_ShowsAssetName()
        {
            AnimationClip clip = CreateTempAsset();
            var nameLabel = new AssetNameLabel(false);

            nameLabel.SetAsset(clip);

            Assert.AreEqual(_assetName, GetLabel(nameLabel).text);
            StringAssert.Contains($"{_tempFolder}/{_assetName}.anim", GetLabel(nameLabel).tooltip);
        }

        [Test]
        public void RefreshName_AfterExternalRename_ShowsNewName()
        {
            AnimationClip clip = CreateTempAsset();
            var nameLabel = new AssetNameLabel(false);
            nameLabel.SetAsset(clip);

            AssetDatabase.RenameAsset($"{_tempFolder}/{_assetName}.anim", "Renamed");
            nameLabel.RefreshName();

            Assert.AreEqual("Renamed", GetLabel(nameLabel).text);
        }

        [UnityTest]
        public IEnumerator DoubleClick_PersistentAsset_SwapsToField()
        {
            AssetNameLabel nameLabel = SetUpAttached(CreateTempAsset());
            yield return null;

            SendClick(GetLabel(nameLabel), 2);
            yield return null;

            Assert.IsTrue(IsEditing(nameLabel));
            Assert.AreEqual(_assetName, GetField(nameLabel).value);
        }

        [UnityTest]
        public IEnumerator SingleClick_DoesNotEdit()
        {
            AssetNameLabel nameLabel = SetUpAttached(CreateTempAsset());
            yield return null;

            SendClick(GetLabel(nameLabel), 1);
            yield return null;

            Assert.IsFalse(IsEditing(nameLabel));
        }

        [UnityTest]
        public IEnumerator DoubleClick_NonPersistentObject_DoesNotEdit()
        {
            var inMemory = new AnimationClip { name = "InMemory" };
            AssetNameLabel nameLabel = SetUpAttached(inMemory);
            yield return null;

            SendClick(GetLabel(nameLabel), 2);
            yield return null;

            Assert.IsFalse(IsEditing(nameLabel));
            Object.DestroyImmediate(inMemory);
        }

        [UnityTest]
        public IEnumerator EditThenLeaveField_RenamesAssetFile()
        {
            AnimationClip clip = CreateTempAsset();
            AssetNameLabel nameLabel = SetUpAttached(clip);
            yield return null;

            SendClick(GetLabel(nameLabel), 2);
            GetField(nameLabel).SetValueWithoutNotify("  NewName  ");
            SendFocusOut(GetField(nameLabel));
            yield return null;

            Assert.AreEqual("NewName", clip.name, "Should trim + rename");
            Assert.AreEqual($"{_tempFolder}/NewName.anim", AssetDatabase.GetAssetPath(clip));
            Assert.AreEqual("NewName", GetLabel(nameLabel).text);
            Assert.IsFalse(IsEditing(nameLabel));
        }

        [UnityTest]
        public IEnumerator Escape_CancelsRename()
        {
            AnimationClip clip = CreateTempAsset();
            AssetNameLabel nameLabel = SetUpAttached(clip);
            yield return null;

            SendClick(GetLabel(nameLabel), 2);
            GetField(nameLabel).SetValueWithoutNotify("NewName");
            PressKey(GetField(nameLabel), KeyCode.Escape);
            SendFocusOut(GetField(nameLabel));
            yield return null;

            Assert.AreEqual(_assetName, clip.name);
            Assert.IsFalse(IsEditing(nameLabel));
        }

        [UnityTest]
        public IEnumerator Rename_ToTakenName_WarnsAndKeepsName()
        {
            AnimationClip clip = CreateTempAsset();
            CreateTempAsset("Taken");
            AssetNameLabel nameLabel = SetUpAttached(clip);
            yield return null;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex($"Could not rename {_assetName} to Taken"));
            SendClick(GetLabel(nameLabel), 2);
            GetField(nameLabel).SetValueWithoutNotify("Taken");
            SendFocusOut(GetField(nameLabel));
            yield return null;

            Assert.AreEqual(_assetName, clip.name);
            Assert.AreEqual(_assetName, GetLabel(nameLabel).text);
        }
        #endregion
    }
}
