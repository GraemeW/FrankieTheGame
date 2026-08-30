using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Covered:
    //  - SavingSystem's methods that depend on GetValidSaveableEntities (live scene scan) and/or file I/O, using TestSaveableComponent on real GameObjects
    // Not Covered:
    //  - LoadLastScene: calls SceneManager.LoadSceneAsync, which switches the active scene (heavier PlayMode/[UnityTest]-style test)
    //  - GetValidSaveableEntities: scans the whole loaded scene, not just this test's own GameObjects (high likelihood for pollution/side effects)
    //    - vs. every test here, which creates its entities in [SetUp] and destroys them in [TearDown]
    public class SavingSystemSceneIntegrationTests
    {
        // Static/Const Tunables
        private const string _tempSaveFileA = "_TEMP_SavingSystemSceneA_SafeToDelete";
        private const string _tempSaveFileB = "_TEMP_SavingSystemSceneB_SafeToDelete";
        private const string _uniqueIdentifierRef = "uniqueIdentifier";

        // State
        private readonly List<GameObject> spawnedGameObjects = new();
        private static string PathFor(string saveFile) => Path.Combine(Application.persistentDataPath, saveFile + ".sav");
        
        #region DataStructures
        private class TestInstantiatingSaveableComponent : MonoBehaviour, ISaveableBase
        {
            public Action onObjectInstantiationRestore;
            public LoadPriority GetLoadPriority() => LoadPriority.ObjectInstantiation;
            public SaveState CaptureState() => null;
            public void RestoreState(SaveState saveState) => onObjectInstantiationRestore?.Invoke();
        }
        
        private (GameObject gameObject, SaveableEntity entity, TestSaveableComponent component) SpawnSaveableEntity(string name, object captureValue, LoadPriority loadPriority = LoadPriority.ObjectProperty, bool isCorePlayerState = false)
        {
            var gameObject = new GameObject(name);
            var entity = gameObject.AddComponent<SaveableEntity>();
            var component = gameObject.AddComponent<TestSaveableComponent>();
            component.captureStateReturns = captureValue;
            component.loadPriority = loadPriority;
            component.isCorePlayerState = isCorePlayerState;

            spawnedGameObjects.Add(gameObject);
            return (gameObject, entity, component);
        }
        
        // Forces a SaveableEntity's private serialized uniqueIdentifier to a chosen value via SerializedObject
        //  - allows a save file built ahead of time can target an entity that doesn't exist yet (e.g. one about to be spawned mid-restore)
        private static void ForceUniqueIdentifier(SaveableEntity entity, string id)
        {
            var serializedEntity = new SerializedObject(entity);
            serializedEntity.FindProperty(_uniqueIdentifierRef).stringValue = id;
            serializedEntity.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion

        #region Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in spawnedGameObjects.Where(gameObject => gameObject != null))
            {
                Object.DestroyImmediate(gameObject);
            }
            spawnedGameObjects.Clear();

            foreach (string saveFile in new[] { _tempSaveFileA, _tempSaveFileB })
            {
                string path = PathFor(saveFile);
                if (File.Exists(path)) { File.Delete(path); }
            }
        }
        #endregion

        #region GetValidSaveableEntities
        [Test]
        public void GetValidSaveableEntities_ExcludesSaveRestrictedEntities()
        {
            (_, SaveableEntity includedEntity, _) = SpawnSaveableEntity("Included", "value");
            (_, SaveableEntity restrictedEntity, _) = SpawnSaveableEntity("Restricted", "value");
            restrictedEntity.ForcePreventSave();

            List<SaveableEntity> result = SavingSystem.GetValidSaveableEntities();

            CollectionAssert.Contains(result, includedEntity);
            CollectionAssert.DoesNotContain(result, restrictedEntity);
        }

        [Test]
        public void GetValidSaveableEntities_IncludesInactiveGameObjects()
        {
            (GameObject inactiveGameObject, SaveableEntity inactiveEntity, _) = SpawnSaveableEntity("Inactive", "value");
            inactiveGameObject.SetActive(false);

            List<SaveableEntity> result = SavingSystem.GetValidSaveableEntities();

            CollectionAssert.Contains(result, inactiveEntity);
        }
        #endregion

        #region Save / LoadWithinScene
        [Test]
        public void Save_WritesEntityStateAndLastScene()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("Player", "value1");

            SavingSystem.Save(_tempSaveFileA);

            JObject fullState = SavingSystem.ManualGetFullState(_tempSaveFileA);
            Assert.IsTrue(fullState.ContainsKey(entity.GetUniqueIdentifier()));
            Assert.AreEqual(SceneManager.GetActiveScene().name, SavingSystem.ManualGetLastScene(fullState));
        }

        [Test]
        public void Save_ThenLoadWithinScene_RestoresComponentAndAppliesFinishingTouches()
        {
            (_, _, TestSaveableComponent component) = SpawnSaveableEntity("Player", "value1", LoadPriority.ObjectProperty);

            SavingSystem.Save(_tempSaveFileA);

            // Simulate a fresh load -- clear what the capture pass already proved
            component.restoreStateWasCalled = false;
            component.applyFinishingTouchesWasCalled = false;

            SavingSystem.LoadWithinScene(_tempSaveFileA);

            Assert.IsTrue(component.restoreStateWasCalled);
            Assert.IsTrue(component.applyFinishingTouchesWasCalled);
            component.restoreStateReceivedValue.TryGetState(out string restoredValue);
            Assert.AreEqual("value1", restoredValue);
        }

        [Test]
        public void LoadWithinScene_ObjectInstantiationSpawnsNewEntity_PropertyAndFinishingTouchesPassesReachIt()
        {
            // Exercising full restore:
            //  - Pass 1 (ObjectInstantiation) on an existing entity spawns a new SaveableEntity mid-restore
            //  - Pass 2 (ObjectProperty) re-scans the scene -> picks up that new entity to restore its properties
            //  - Pass 3 (FinishingTouches) -> must reach instantiated object as well
            
            (GameObject parentGameObject, SaveableEntity parentEntity, _) = SpawnSaveableEntity("Parent", null);
            var instantiator = parentGameObject.AddComponent<TestInstantiatingSaveableComponent>();

            string spawnedEntityId = Guid.NewGuid().ToString();
            TestSaveableComponent spawnedComponent = null;

            instantiator.onObjectInstantiationRestore = () =>
            {
                var spawnedGameObject = new GameObject("SpawnedDuringInstantiation");
                spawnedGameObjects.Add(spawnedGameObject);
                var spawnedEntity = spawnedGameObject.AddComponent<SaveableEntity>();
                spawnedComponent = spawnedGameObject.AddComponent<TestSaveableComponent>();
                spawnedComponent.loadPriority = LoadPriority.ObjectProperty;
                ForceUniqueIdentifier(spawnedEntity, spawnedEntityId);
            };

            var fullState = new JObject
            {
                [parentEntity.GetUniqueIdentifier()] = new JObject { [typeof(TestInstantiatingSaveableComponent).ToString()] = JToken.FromObject(new SaveState(LoadPriority.ObjectInstantiation, true)) },
                [spawnedEntityId] = new JObject { [typeof(TestSaveableComponent).ToString()] = JToken.FromObject(new SaveState(LoadPriority.ObjectProperty, "restoredAfterSpawn")) }
            };
            SavingSystem.ManualSave(_tempSaveFileA, fullState);

            SavingSystem.LoadWithinScene(_tempSaveFileA);

            Assert.IsNotNull(spawnedComponent, "ObjectInstantiation pass should have spawned the new entity");
            Assert.IsTrue(spawnedComponent.restoreStateWasCalled, "ObjectProperty pass should reach the newly-spawned entity");
            Assert.IsTrue(spawnedComponent.applyFinishingTouchesWasCalled, "Finishing touches should reach the newly-spawned entity");
            spawnedComponent.restoreStateReceivedValue.TryGetState(out string restoredValue);
            Assert.AreEqual("restoredAfterSpawn", restoredValue);
        }
        #endregion

        #region Append
        [Test]
        public void Append_WritesOnlyTheGivenEntity()
        {
            (_, SaveableEntity targetEntity, _) = SpawnSaveableEntity("Target", "appendedValue");
            SpawnSaveableEntity("Other", "otherValue"); // present in the scene but not passed to Append

            SavingSystem.Append(_tempSaveFileA, targetEntity);

            JObject fullState = SavingSystem.ManualGetFullState(_tempSaveFileA);
            Assert.IsTrue(fullState.ContainsKey(targetEntity.GetUniqueIdentifier()));
            Assert.AreEqual(1, fullState.Properties().Count());
        }

        [Test]
        public void Append_SaveRestrictedEntity_WritesNoEntry()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("Restricted", "value");
            entity.ForcePreventSave();

            SavingSystem.Append(_tempSaveFileA, entity);

            JObject fullState = SavingSystem.ManualGetFullState(_tempSaveFileA);
            Assert.IsFalse(fullState.ContainsKey(entity.GetUniqueIdentifier()));
        }

        [Test]
        public void Append_NullEntity_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SavingSystem.Append(_tempSaveFileA, null));
        }
        #endregion

        #region Copy variants
        [Test]
        public void CopySessionToSave_CapturesLiveEntitiesIntoSaveFile()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("Player", "value1");

            SavingSystem.CopySessionToSave(_tempSaveFileA, _tempSaveFileB);

            JObject savedState = SavingSystem.ManualGetFullState(_tempSaveFileB);
            Assert.IsTrue(savedState.ContainsKey(entity.GetUniqueIdentifier()));
        }

        [Test]
        public void CopySaveToSession_CapturesLiveEntitiesIntoSessionFile()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("Player", "value1");

            SavingSystem.CopySaveToSession(_tempSaveFileA, _tempSaveFileB);

            JObject sessionState = SavingSystem.ManualGetFullState(_tempSaveFileB);
            Assert.IsTrue(sessionState.ContainsKey(entity.GetUniqueIdentifier()));
        }

        [Test]
        public void CopyCorePlayerStateToSave_OnlyCapturesCorePlayerStateComponents()
        {
            (_, SaveableEntity coreEntity, _) = SpawnSaveableEntity("Core", "coreValue", isCorePlayerState: true);
            (_, SaveableEntity nonCoreEntity, _) = SpawnSaveableEntity("NonCore", "nonCoreValue", isCorePlayerState: false);

            SavingSystem.CopyCorePlayerStateToSave(_tempSaveFileA);

            JObject savedState = SavingSystem.ManualGetFullState(_tempSaveFileA);
            Assert.IsTrue(savedState.ContainsKey(coreEntity.GetUniqueIdentifier()));
            Assert.IsFalse(savedState.ContainsKey(nonCoreEntity.GetUniqueIdentifier()));
            // onlyCorePlayerState skips the last-scene stamp entirely - see SavingSystem.CaptureState
            Assert.IsFalse(savedState.ContainsKey("lastSceneBuildIndex"));
        }
        #endregion

        #region ManualGetStateEntityToken
        [Test]
        public void ManualGetStateEntityToken_ExistingEntry_ReturnsToken()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("Player", "value1");
            SavingSystem.Save(_tempSaveFileA);

            JToken token = SavingSystem.ManualGetStateEntityToken(_tempSaveFileA, entity);

            Assert.IsNotNull(token);
        }

        [Test]
        public void ManualGetStateEntityToken_NoMatchingEntry_ReturnsNull()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("Player", "value1");
            SavingSystem.ManualSave(_tempSaveFileA, new JObject()); // save file with no entry for this entity

            JToken token = SavingSystem.ManualGetStateEntityToken(_tempSaveFileA, entity);

            Assert.IsNull(token);
        }
        #endregion
    }
}
