using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Covered:
    //  - file-touching methods that operate purely on JObject state (ManualSave, ManualGetFullState, Delete, ListSaves, CopySaveToSave)
    // Not Covered:
    //  - GetValidSaveableEntities-dependent methods (Save, Append, the other Copy* variants, LoadWithinScene, LoadLastScene)
    //    -> deferred to the SaveableEntity/ISaveable test-double pass
    
    public class SavingSystemFileOperationsTests
    {
        // Distinct disposable names per test to avoid cross-test file collisions
        private const string _tempSaveFileA = "_TEMP_SavingSystemFileOpsA_SafeToDelete";
        private const string _tempSaveFileB = "_TEMP_SavingSystemFileOpsB_SafeToDelete";

        private static string PathFor(string saveFile) => Path.Combine(Application.persistentDataPath, saveFile + ".sav");

        [TearDown]
        public void TearDown()
        {
            foreach (string saveFile in new[] { _tempSaveFileA, _tempSaveFileB })
            {
                string path = PathFor(saveFile);
                if (File.Exists(path)) { File.Delete(path); }
            }
        }

        [Test]
        public void ManualGetFullState_NonexistentFile_ReturnsEmptyJObject()
        {
            JObject state = SavingSystem.ManualGetFullState(_tempSaveFileA);

            Assert.IsNotNull(state);
            Assert.IsFalse(state.Properties().Any());
        }

        [Test]
        public void Delete_RemovesFileFromDisk()
        {
            SavingSystem.ManualSave(_tempSaveFileA, new JObject { ["key"] = "value" });
            Assert.IsTrue(File.Exists(PathFor(_tempSaveFileA)));

            SavingSystem.Delete(_tempSaveFileA);

            Assert.IsFalse(File.Exists(PathFor(_tempSaveFileA)));
        }

        [Test]
        public void ListSaves_ReflectsSaveAndDelete()
        {
            SavingSystem.ManualSave(_tempSaveFileA, new JObject());
            CollectionAssert.Contains(SavingSystem.ListSaves().ToList(), _tempSaveFileA);

            SavingSystem.Delete(_tempSaveFileA);
            CollectionAssert.DoesNotContain(SavingSystem.ListSaves().ToList(), _tempSaveFileA);
        }

        [Test]
        public void CopySaveToSave_DuplicatesStateUnderNewFileName()
        {
            var originalState = new JObject { ["someKey"] = "someValue" };
            SavingSystem.ManualSave(_tempSaveFileA, originalState);

            SavingSystem.CopySaveToSave(_tempSaveFileA, _tempSaveFileB);

            Assert.IsTrue(File.Exists(PathFor(_tempSaveFileB)));
            JObject copiedState = SavingSystem.ManualGetFullState(_tempSaveFileB);
            Assert.AreEqual("someValue", copiedState["someKey"]?.ToObject<string>());
        }
    }
}
