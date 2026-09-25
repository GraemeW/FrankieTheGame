using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class SceneReferencePropertyDrawerTests
    {
        // Const Tunables
        private const string _rootFolderName = "ScratchTest_SceneReferenceDrawer_SafeToDelete";
        private const string _rootFolder = "Assets/" + _rootFolderName;
        private const string _alphaName = "ScratchDrawer_Alpha";
        private const string _alphaExtendedName = "ScratchDrawer_AlphaExtended";
        private const string _betaName = "ScratchDrawer_Beta";
        private const string _dupName = "ScratchDrawer_Dup";
        private const string _alphaPath = _rootFolder + "/" + _alphaName + ".unity";
        private const string _alphaExtendedPath = _rootFolder + "/" + _alphaExtendedName + ".unity";
        private const string _betaPath = _rootFolder + "/" + _betaName + ".unity";
        private const string _dupPathOne = _rootFolder + "/Dup1/" + _dupName + ".unity";
        private const string _dupPathTwo = _rootFolder + "/Dup2/" + _dupName + ".unity";
        private const string _prefabPath = _rootFolder + "/ScratchDrawerHost.prefab";
        private const string _missingPath = "Assets/ScratchDrawer_DoesNotExist/Nope.unity";
        private const double _trackingTimeoutSeconds = 3d;

        // State
        private SceneAsset alphaScene;
        private SceneAsset alphaExtendedScene;
        private SceneAsset betaScene;
        private readonly List<GameObject> spawnedObjects = new();
        private readonly List<SerializedObject> serializedObjects = new();
        private EditorWindow window;

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (AssetDatabase.IsValidFolder(_rootFolder)) { AssetDatabase.DeleteAsset(_rootFolder); }
            AssetDatabase.CreateFolder("Assets", _rootFolderName);
            AssetDatabase.CreateFolder(_rootFolder, "Dup1");
            AssetDatabase.CreateFolder(_rootFolder, "Dup2");

            alphaScene = CreateSceneAsset(_alphaPath);
            alphaExtendedScene = CreateSceneAsset(_alphaExtendedPath);
            betaScene = CreateSceneAsset(_betaPath);
            CreateSceneAsset(_dupPathOne);
            CreateSceneAsset(_dupPathTwo);
            AssetDatabase.Refresh();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(_rootFolder);
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
            foreach (SerializedObject serializedObject in serializedObjects) { serializedObject.Dispose(); }
            serializedObjects.Clear();
            foreach (GameObject spawnedObject in spawnedObjects)
            {
                if (spawnedObject != null) { Object.DestroyImmediate(spawnedObject); }
            }
            spawnedObjects.Clear();
            AssetDatabase.DeleteAsset(_prefabPath);
        }
        #endregion

        #region PrivateMethods
        private static SceneAsset CreateSceneAsset(string path)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        private SceneReferenceHost CreateHost(SceneAsset sceneAsset, string sceneName, string scenePath)
        {
            var hostObject = new GameObject("SceneReferenceHost");
            spawnedObjects.Add(hostObject);

            var host = hostObject.AddComponent<SceneReferenceHost>();
            host.sceneReference = new SceneReference(sceneName) { sceneAsset = sceneAsset, scenePath = scenePath };
            return host;
        }

        private SerializedProperty GetSceneReferenceProperty(params Object[] targets)
        {
            var serializedObject = new SerializedObject(targets);
            serializedObjects.Add(serializedObject);
            return serializedObject.FindProperty(nameof(SceneReferenceHost.sceneReference));
        }

        private static VisualElement BuildDrawerUI(SerializedProperty property) => new SceneReferencePropertyDrawer().CreatePropertyGUI(property);

        private void Attach(VisualElement element)
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            window.rootVisualElement.Add(element);
        }

        private static void AssertReference(SceneReferenceHost host, SceneAsset expectedAsset, string expectedName, string expectedPath)
        {
            if (expectedAsset == null) { Assert.IsTrue(host.sceneReference.sceneAsset == null, "sceneAsset should be empty"); }
            else { Assert.AreEqual(expectedAsset, host.sceneReference.sceneAsset); }
            Assert.AreEqual(expectedName, host.sceneReference.SceneName);
            Assert.AreEqual(expectedPath, host.sceneReference.scenePath);
        }

        private static Regex RepairedLog(string path) => new("^Repaired scene reference for .*: " + Regex.Escape(path) + "$");
        #endregion

        #region StructureTests
        [Test]
        public void CreatePropertyGUI_BuildsRowWithDrawerFields()
        {
            SceneReferenceHost host = CreateHost(alphaScene, _alphaName, _alphaPath);
            SerializedProperty property = GetSceneReferenceProperty(host);

            VisualElement root = BuildDrawerUI(property);
            
            Assert.AreEqual(3, root.childCount);
            var field = root[0] as ObjectField;
            VisualElement indicator = root[1];
            var clearButton = root[2] as Button;
            Assert.IsNotNull(field);
            Assert.IsNotNull(indicator);
            Assert.IsNotNull(clearButton);

            Assert.AreEqual(typeof(SceneAsset), field.objectType);
            Assert.IsFalse(field.allowSceneObjects);
            Assert.AreEqual(property.displayName, field.label);
            Assert.IsTrue(field.ClassListContains(BaseField<Object>.alignedFieldUssClassName));
        }

        [Test]
        public void CreatePropertyGUI_SceneAssetFieldIsDeliberatelyUnbound()
        {
            SceneReferenceHost host = CreateHost(alphaScene, _alphaName, _alphaPath);

            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(host));

            Assert.IsTrue(string.IsNullOrEmpty(root.Q<ObjectField>().bindingPath));
        }

        [Test]
        public void CreatePropertyGUI_DoesNotExposeSceneNameOrPathForEditing()
        {
            SceneReferenceHost host = CreateHost(alphaScene, _alphaName, _alphaPath);

            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(host));

            Assert.AreEqual(0, root.Query<TextField>().ToList().Count);
        }

        [Test]
        public void CreatePropertyGUI_ExistingSceneAsset_ShowsItInFieldAndLeavesReferenceUntouched()
        {
            SceneReferenceHost host = CreateHost(alphaScene, _alphaName, _alphaPath);

            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(host));

            Assert.AreEqual(alphaScene, root.Q<ObjectField>().value);
            AssertReference(host, alphaScene, _alphaName, _alphaPath);
        }

        [Test]
        public void CreatePropertyGUI_MultipleObjectsWithDifferentScenes_ShowsMixedValue()
        {
            SceneReferenceHost hostA = CreateHost(alphaScene, _alphaName, _alphaPath);
            SceneReferenceHost hostB = CreateHost(betaScene, _betaName, _betaPath);

            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(hostA, hostB));

            Assert.IsTrue(root.Q<ObjectField>().showMixedValue);
        }

        [Test]
        public void CreatePropertyGUI_MultipleObjectsWithSameScene_DoesNotShowMixedValue()
        {
            SceneReferenceHost hostA = CreateHost(alphaScene, _alphaName, _alphaPath);
            SceneReferenceHost hostB = CreateHost(alphaScene, _alphaName, _alphaPath);

            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(hostA, hostB));

            Assert.IsFalse(root.Q<ObjectField>().showMixedValue);
        }
        #endregion

        #region RelinkTests
        [Test]
        public void CreatePropertyGUI_NullAssetWithValidPath_ReLinksAssetAndResyncsNameAndPath()
        {
            SceneReferenceHost host = CreateHost(null, "StaleName", _alphaPath);
            SerializedProperty property = GetSceneReferenceProperty(host);

            LogAssert.Expect(LogType.Log, RepairedLog(_alphaPath));
            VisualElement root = BuildDrawerUI(property);

            AssertReference(host, alphaScene, _alphaName, _alphaPath);
            Assert.AreEqual(alphaScene, root.Q<ObjectField>().value);
        }

        [Test]
        public void CreatePropertyGUI_NullAssetWithStalePathButValidName_ReLinksByNameAndResyncsPath()
        {
            SceneReferenceHost host = CreateHost(null, _betaName, _missingPath);
            SerializedProperty property = GetSceneReferenceProperty(host);

            LogAssert.Expect(LogType.Log, RepairedLog(_betaPath));
            VisualElement root = BuildDrawerUI(property);

            AssertReference(host, betaScene, _betaName, _betaPath);
            Assert.AreEqual(betaScene, root.Q<ObjectField>().value);
        }

        [Test]
        public void CreatePropertyGUI_NullAssetWithNameOnly_ReLinksToExactNameMatchNotLongerNames()
        {
            // "ScratchDrawer_AlphaExtended" also matches the asset search for "ScratchDrawer_Alpha", but isn't an exact name match
            SceneReferenceHost host = CreateHost(null, _alphaName, string.Empty);
            SerializedProperty property = GetSceneReferenceProperty(host);

            LogAssert.Expect(LogType.Log, RepairedLog(_alphaPath));
            BuildDrawerUI(property);

            AssertReference(host, alphaScene, _alphaName, _alphaPath);
            Assert.AreNotEqual(alphaExtendedScene, host.sceneReference.sceneAsset);
        }

        [Test]
        public void CreatePropertyGUI_NullAssetWithDuplicateSceneNames_WarnsAndLeavesReferenceUnlinked()
        {
            SceneReferenceHost host = CreateHost(null, _dupName, string.Empty);
            SerializedProperty property = GetSceneReferenceProperty(host);

            LogAssert.Expect(LogType.Warning, new Regex("^Attempting to repair scene reference for " + _dupName + ", but duplicate scene references found"));
            LogAssert.Expect(LogType.Warning, new Regex("^Could not repair scene reference for "));
            VisualElement root = BuildDrawerUI(property);

            Assert.IsTrue(host.sceneReference.sceneAsset == null);
            Assert.AreEqual(_dupName, host.sceneReference.SceneName);
            Assert.IsTrue(root.Q<ObjectField>().value == null);
        }

        [Test]
        public void CreatePropertyGUI_NullAssetWithNothingFindable_WarnsAndLeavesReferenceUnlinked()
        {
            SceneReferenceHost host = CreateHost(null, "ScratchDrawer_DoesNotExist", _missingPath);
            SerializedProperty property = GetSceneReferenceProperty(host);

            LogAssert.Expect(LogType.Warning, new Regex("^Could not repair scene reference for .* \\(path: '" + Regex.Escape(_missingPath) + "', name: 'ScratchDrawer_DoesNotExist'\\)$"));
            BuildDrawerUI(property);

            Assert.IsTrue(host.sceneReference.sceneAsset == null);
            Assert.AreEqual("ScratchDrawer_DoesNotExist", host.sceneReference.SceneName);
            Assert.AreEqual(_missingPath, host.sceneReference.scenePath);
        }

        [Test]
        public void CreatePropertyGUI_NeverSetReference_DoesNotAttemptRepair()
        {
            SceneReferenceHost host = CreateHost(null, null, null);

            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(host));

            LogAssert.NoUnexpectedReceived(); // An attempted repair would have warned that nothing could be found
            Assert.IsTrue(host.sceneReference.sceneAsset == null);
            Assert.IsFalse(host.sceneReference.IsSet());
            Assert.IsTrue(root.Q<ObjectField>().value == null);
        }

        [Test]
        public void CreatePropertyGUI_MultipleObjectsSelected_DoesNotRelinkEither()
        {
            SceneReferenceHost hostA = CreateHost(null, _alphaName, _alphaPath);
            SceneReferenceHost hostB = CreateHost(null, _alphaName, _alphaPath);

            BuildDrawerUI(GetSceneReferenceProperty(hostA, hostB));

            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(hostA.sceneReference.sceneAsset == null);
            Assert.IsTrue(hostB.sceneReference.sceneAsset == null);
        }

        [Test]
        public void CreatePropertyGUI_PrefabInstance_DoesNotRelinkOrAddOverride()
        {
            SceneReferenceHost prefabSource = CreateHost(null, _alphaName, _alphaPath);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabSource.gameObject, _prefabPath);
            var instanceObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            spawnedObjects.Add(instanceObject);
            var instanceHost = instanceObject.GetComponent<SceneReferenceHost>();

            BuildDrawerUI(GetSceneReferenceProperty(instanceHost));

            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(instanceHost.sceneReference.sceneAsset == null);
            Assert.IsFalse(PrefabUtility.HasPrefabInstanceAnyOverrides(instanceObject, false));
        }

        [Test]
        public void CreatePropertyGUI_PrefabAsset_HealsTheAssetReference()
        {
            SceneReferenceHost prefabSource = CreateHost(null, _alphaName, _alphaPath);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabSource.gameObject, _prefabPath);
            var prefabHost = prefabAsset.GetComponent<SceneReferenceHost>();

            LogAssert.Expect(LogType.Log, RepairedLog(_alphaPath));
            BuildDrawerUI(GetSceneReferenceProperty(prefabHost));

            Assert.AreEqual(alphaScene, prefabHost.sceneReference.sceneAsset);
        }
        #endregion

        #region CallbackTests
        [UnityTest]
        public IEnumerator AssetFieldChanged_ToScene_WritesAssetNameAndPath()
        {
            SceneReferenceHost host = CreateHost(alphaScene, _alphaName, _alphaPath);
            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(host));
            Attach(root);
            yield return null;

            root.Q<ObjectField>().value = betaScene;
            yield return null;

            AssertReference(host, betaScene, _betaName, _betaPath);
        }

        [UnityTest]
        public IEnumerator AssetFieldChanged_ToNull_ClearsAssetNameAndPath()
        {
            SceneReferenceHost host = CreateHost(alphaScene, _alphaName, _alphaPath);
            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(host));
            Attach(root);
            yield return null;

            root.Q<ObjectField>().value = null;
            yield return null;

            AssertReference(host, null, string.Empty, string.Empty);
        }

        [UnityTest]
        public IEnumerator AssetFieldChanged_MultipleDifferentValues_AppliesToEveryTarget()
        {
            SceneReferenceHost hostA = CreateHost(alphaScene, _alphaName, _alphaPath);
            SceneReferenceHost hostB = CreateHost(betaScene, _betaName, _betaPath);
            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(hostA, hostB));
            Attach(root);
            yield return null;

            root.Q<ObjectField>().value = alphaExtendedScene;
            yield return null;

            AssertReference(hostA, alphaExtendedScene, _alphaExtendedName, _alphaExtendedPath);
            AssertReference(hostB, alphaExtendedScene, _alphaExtendedName, _alphaExtendedPath);
        }

        [UnityTest]
        public IEnumerator ClearButtonClick_ClearsReferenceAndField()
        {
            SceneReferenceHost host = CreateHost(alphaScene, _alphaName, _alphaPath);
            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(host));
            Attach(root);
            yield return null;

            HeadlessEditorWindow.SendClick(root.Q<Button>());
            yield return null;

            AssertReference(host, null, string.Empty, string.Empty);
            Assert.IsTrue(root.Q<ObjectField>().value == null);
        }

        [UnityTest]
        public IEnumerator ClearButtonClick_WhenAlreadyEmpty_LeavesReferenceEmpty()
        {
            SceneReferenceHost host = CreateHost(null, null, null);
            VisualElement root = BuildDrawerUI(GetSceneReferenceProperty(host));
            Attach(root);
            yield return null;

            HeadlessEditorWindow.SendClick(root.Q<Button>());
            yield return null;

            Assert.IsTrue(host.sceneReference.sceneAsset == null);
            Assert.IsFalse(host.sceneReference.IsSet());
            Assert.IsTrue(root.Q<ObjectField>().value == null);
        }

        [UnityTest]
        public IEnumerator ExternalChangeToSceneAsset_RefreshesFieldViaPropertyTracking()
        {
            SceneReferenceHost host = CreateHost(alphaScene, _alphaName, _alphaPath);
            SerializedProperty property = GetSceneReferenceProperty(host);
            VisualElement root = BuildDrawerUI(property);
            Attach(root);
            yield return null;
            var field = root.Q<ObjectField>();

            // Changed through a second SerializedObject, like an inspector elsewhere editing the same object
            var otherSerializedObject = new SerializedObject(host);
            serializedObjects.Add(otherSerializedObject);
            otherSerializedObject.FindProperty("sceneReference.sceneAsset").objectReferenceValue = betaScene;
            otherSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            // Property tracking polls on the editor's schedule, so wait (bounded) rather than assuming a frame count
            double deadline = EditorApplication.timeSinceStartup + _trackingTimeoutSeconds;
            while (field.value != betaScene && EditorApplication.timeSinceStartup < deadline)
            {
                property.serializedObject.Update();
                yield return null;
            }

            Assert.AreEqual(betaScene, field.value);
        }
        #endregion
    }
}
