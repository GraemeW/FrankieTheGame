using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Saving.Tests.Editor
{
    public class SavingSystemSceneLoadTests
    {
        // Tested:
        //  - LoadLastScene - hands the saved scene key to the given scene loader; null loader errors + stops
        //  - LoadLastScene - no saved scene, or a loader reporting failure, warns + skips RestoreState
        //  - ManualUpdateLastScene - warns only when the editor hook reports the scene as not loadable
        // Not Tested:
        //  - LoadLastScene's successful restore (RestoreState) - needs a loaded scene's saveable entities (stopped after the first step here)

        // Const
        private const string _tempSaveFile = "_TEMP_SavingSystemSceneLoad_SafeToDelete";
        private const string _savedSceneName = "SavingSystemSceneLoadTests_SavedScene";

        // State
        private System.Func<string, bool> originalIsSceneLoadable;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalIsSceneLoadable = SavingSystem.editorIsSceneLoadable;
            SavingSystem.editorIsSceneLoadable = null;
        }

        [TearDown]
        public void TearDown()
        {
            SavingSystem.editorIsSceneLoadable = originalIsSceneLoadable;
            string path = Path.Combine(Application.persistentDataPath, _tempSaveFile + ".sav");
            if (File.Exists(path)) { File.Delete(path); }
        }
        #endregion

        #region Helpers
        private static IEnumerator EmptyLoad() { yield break; }
        #endregion

        #region Tests
        [Test]
        public void LoadLastScene_NullLoader_LogsErrorAndStops()
        {
            LogAssert.Expect(LogType.Error, new Regex("No scene loader given"));

            IEnumerator load = SavingSystem.LoadLastScene(_tempSaveFile, null);

            Assert.IsFalse(load.MoveNext());
        }

        [Test]
        public void LoadLastScene_SavedScene_PassesSavedSceneNameToLoader()
        {
            var state = new JObject();
            SavingSystem.ManualUpdateLastScene(state, _savedSceneName);
            SavingSystem.ManualSave(_tempSaveFile, state);
            string requestedScene = null;
            IEnumerator loaderStep = EmptyLoad();

            IEnumerator load = SavingSystem.LoadLastScene(_tempSaveFile, (sceneKey, _) => { requestedScene = sceneKey; return loaderStep; });

            Assert.IsTrue(load.MoveNext());
            Assert.AreEqual(_savedSceneName, requestedScene);
            Assert.AreSame(loaderStep, load.Current);
        }

        [Test]
        public void LoadLastScene_NoSavedScene_WarnsAndSkipsLoad()
        {
            SavingSystem.ManualSave(_tempSaveFile, new JObject());
            var isLoaderCalled = false;
            LogAssert.Expect(LogType.Warning, new Regex("has no last scene, so its state wasn't restored"));

            IEnumerator load = SavingSystem.LoadLastScene(_tempSaveFile, (_, _) => { isLoaderCalled = true; return EmptyLoad(); });

            Assert.IsFalse(load.MoveNext());
            Assert.IsFalse(isLoaderCalled);
        }

        [Test]
        public void LoadLastScene_LoaderReportsFailure_WarnsAndSkipsRestore()
        {
            var state = new JObject();
            SavingSystem.ManualUpdateLastScene(state, _savedSceneName);
            SavingSystem.ManualSave(_tempSaveFile, state);
            LogAssert.Expect(LogType.Warning, new Regex("couldn't load its scene .*so its state wasn't restored"));

            IEnumerator load = SavingSystem.LoadLastScene(_tempSaveFile, (_, onLoadComplete) => { onLoadComplete(false); return EmptyLoad(); });

            Assert.IsTrue(load.MoveNext()); // The loader step
            Assert.IsFalse(load.MoveNext()); // Stops after the warning ~ i.e. no RestoreState
        }

        [Test]
        public void ManualUpdateLastScene_HookReportsNotLoadable_LogsWarning()
        {
            SavingSystem.editorIsSceneLoadable = _ => false;
            LogAssert.Expect(LogType.Warning, new Regex($"Last scene set to '{_savedSceneName}'"));

            SavingSystem.ManualUpdateLastScene(new JObject(), _savedSceneName);
        }

        [Test]
        public void ManualUpdateLastScene_HookReportsLoadable_NoWarning()
        {
            SavingSystem.editorIsSceneLoadable = _ => true;

            SavingSystem.ManualUpdateLastScene(new JObject(), _savedSceneName);

            LogAssert.NoUnexpectedReceived();
        }
        #endregion
    }
}
