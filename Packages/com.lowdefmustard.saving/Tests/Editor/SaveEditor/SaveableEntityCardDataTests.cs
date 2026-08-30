using LowDefMustard.Saving.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Note:  constructor subscribes to the static Selection.selectionChanged event and never unsubscribes
    //  - no issue since OnEntitySelected guards on saveableEntity == null -> true once DestroyImmediate has run
    //  - however every instance built during a real Editor session leaks a subscription until domain reload
    // Note:  TestBoolSaveableComponent/TestPlayerMoverSaveableComponent are registered once in [OneTimeSetUp] and unregistered in [OneTimeTearDown] via UnregisterSubCard<T>,
    //  - ensures the static sub-card factory registry is clean once this finishes
    
    public class SaveableEntityCardDataTests
    {
        // Const/Static Tunables
        private const string _tempSaveFile = "_TEMP_SaveableEntityCardDataSave_SafeToDelete";
        
        // State
        private readonly List<GameObject> spawnedGameObjects = new();
        private HeadlessEditorWindowTestHelper windowHelper;
        
        #region DataStructures
        private class TestBoolSaveableComponent : MonoBehaviour, ISaveable<bool>
        {
            public bool value;
            public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
            public SaveState CaptureState() => new SaveState(LoadPriority.ObjectProperty, value);
            public void RestoreState(SaveState saveState) { }
            public SaveState ManualGetStateFromData(bool data) => new SaveState(LoadPriority.ObjectProperty, data);

            public bool TryManualGetDataFromState(SaveState saveState, out bool outValue)
            {
                if (saveState == null) { outValue = value; return true; }
                return saveState.TryGetState(out outValue);
            }
        }

        // Exposes a public trigger for RaiseSaveStateChanged so tests can desync it directly
        private class PlayerMoverMarkerSubCard : SaveableSubCardData
        {
            public PlayerMoverMarkerSubCard(ISaveableBase saveable, SaveState saveState)
            {
                this.saveable = saveable;
                this.saveState = saveState;
            }

            public override bool IsPlayerMoverSubCard() => true;
            public void TestTriggerStateChanged() => RaiseSaveStateChanged();
            protected override void AddEditableFieldsToSubCardView(Box subCardView) { }
        }

        private class TestPlayerMoverSaveableComponent : MonoBehaviour, ISaveableBase
        {
            public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
            public SaveState CaptureState() => new SaveState(LoadPriority.ObjectProperty, 0);
            public void RestoreState(SaveState saveState) { }
        }
        #endregion

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SaveableSubCardData.RegisterSubCard<TestBoolSaveableComponent>((saveable, state) => new SimpleBoolSaveableSubCard(saveable, state));
            SaveableSubCardData.RegisterSubCard<TestPlayerMoverSaveableComponent>((saveable, state) => new PlayerMoverMarkerSubCard(saveable, state));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SaveableSubCardData.UnregisterSubCard<TestBoolSaveableComponent>();
            SaveableSubCardData.UnregisterSubCard<TestPlayerMoverSaveableComponent>();
        }

        private GameObject NewGameObject(string name)
        {
            var gameObject = new GameObject(name);
            spawnedGameObjects.Add(gameObject);
            return gameObject;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in spawnedGameObjects.Where(gameObject => gameObject != null))
            {
                Object.DestroyImmediate(gameObject);
            }
            spawnedGameObjects.Clear();

            windowHelper?.Close();
            windowHelper = null;

            string path = Path.Combine(Application.persistentDataPath, _tempSaveFile + ".sav");
            if (File.Exists(path)) { File.Delete(path); }
        }
        #endregion

        #region Constructor
        [Test]
        public void Constructor_NullSaveableEntity_DoesNotThrowAndHasNoSubCards()
        {
            SaveableEntityCardData cardData = null;
            Assert.DoesNotThrow(() =>
            {
                cardData = new SaveableEntityCardData(null, new JObject(), new HashSet<string>(), null, null);
            });
            Assert.IsFalse(cardData.TryGetSaveableSubCardData(out GenericSaveableSubCard _));
        }

        [Test]
        public void Constructor_SetsEntityIdFromUniqueIdentifier()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);

            Assert.AreEqual(entity.GetUniqueIdentifier(), cardData.entityID);
        }

        [Test]
        public void Constructor_AddsEntityIdToGuidsSet()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            var guids = new HashSet<string>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), guids, null, null);

            CollectionAssert.Contains(guids, cardData.entityID);
        }

        [Test]
        public void Constructor_NoParent_EntityNameIsGameObjectName()
        {
            GameObject gameObject = NewGameObject("StandaloneEntity");
            var entity = gameObject.AddComponent<SaveableEntity>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);
            Box card = cardData.DrawSaveableEntityCard(null);

            IEnumerable<string> labels = card.Query<Label>().ToList().Select(label => label.text);
            CollectionAssert.Contains(labels, "GameObject:  StandaloneEntity");
        }

        [Test]
        public void Constructor_WithParent_EntityNamePrefixedWithParentName()
        {
            GameObject parentGameObject = NewGameObject("ParentEntity");
            GameObject childGameObject = NewGameObject("ChildEntity");
            childGameObject.transform.SetParent(parentGameObject.transform);
            var entity = childGameObject.AddComponent<SaveableEntity>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);
            Box card = cardData.DrawSaveableEntityCard(null);

            IEnumerable<string> labels = card.Query<Label>().ToList().Select(label => label.text);
            CollectionAssert.Contains(labels, "GameObject:  ParentEntity/ChildEntity");
        }

        [Test]
        public void Constructor_BuildsGenericFallbackSubCardForUnregisteredComponent()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestSaveableComponent>().captureStateReturns = "value";

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);

            Assert.IsTrue(cardData.TryGetSaveableSubCardData(out GenericSaveableSubCard _));
        }

        [Test]
        public void Constructor_PreExistingCachedState_LoadsIntoSubCardSaveState()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestBoolSaveableComponent>();

            // Build a cached full-save-state matching this entity's real ID ahead of time
            entity.TryCaptureState(null, out JToken capturedState);
            var cachedFullSaveState = new JObject { [entity.GetUniqueIdentifier()] = capturedState };
            var freshComponent = gameObject.GetComponent<TestBoolSaveableComponent>();
            freshComponent.value = true; // capture above used the component's default (false) - distinguish load from a fresh capture

            var cardData = new SaveableEntityCardData(entity, cachedFullSaveState, new HashSet<string>(), null, null);

            cardData.TryGetSaveableSubCardData(out SimpleBoolSaveableSubCard subCard);
            subCard.saveState.TryGetState(out bool loadedValue);
            Assert.IsFalse(loadedValue); // the value captured into cachedFullSaveState before freshComponent.value was flipped
        }
        #endregion

        #region TryGetSaveableSubCardData
        [Test]
        public void TryGetSaveableSubCardData_NoMatchingType_ReturnsFalse()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestSaveableComponent>().captureStateReturns = "value";

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);

            Assert.IsFalse(cardData.TryGetSaveableSubCardData(out SimpleBoolSaveableSubCard _));
        }
        #endregion

        #region ResetSaveableSyncFlag
        [Test]
        public void ResetSaveableSyncFlag_ResyncsDesyncedSubCard()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestBoolSaveableComponent>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);
            cardData.TryGetSaveableSubCardData(out SimpleBoolSaveableSubCard subCard);

            // Force a desync the same way a real edit would - through the live UI field
            Box card = cardData.DrawSaveableEntityCard(null);
            windowHelper = new HeadlessEditorWindowTestHelper();
            windowHelper.Attach(card);
            Toggle toggle = card.Query<Toggle>().First();
            toggle.value = !toggle.value;
            Assert.IsFalse(subCard.IsSaveStateSynced());

            cardData.ResetSaveableSyncFlag();

            Assert.IsTrue(subCard.IsSaveStateSynced());
        }
        #endregion

        #region RemoveFromGUIDs
        [Test]
        public void RemoveFromGUIDs_ExistingId_RemovesAndReturnsTrue()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            var guids = new HashSet<string>();
            var cardData = new SaveableEntityCardData(entity, new JObject(), guids, null, null);

            bool result = cardData.RemoveFromGUIDs(cardData.entityID);

            Assert.IsTrue(result);
            CollectionAssert.DoesNotContain(guids, cardData.entityID);
        }

        [Test]
        public void RemoveFromGUIDs_AlreadyRemoved_ReturnsFalse()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            var guids = new HashSet<string>();
            var cardData = new SaveableEntityCardData(entity, new JObject(), guids, null, null);
            cardData.RemoveFromGUIDs(cardData.entityID);

            bool result = cardData.RemoveFromGUIDs(cardData.entityID);

            Assert.IsFalse(result);
        }
        #endregion

        #region SaveSaveableEntity
        [Test]
        public void SaveSaveableEntity_WithoutFileWrite_UpdatesCachedStateOnly()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestBoolSaveableComponent>();

            var cachedFullSaveState = new JObject();
            var cardData = new SaveableEntityCardData(entity, cachedFullSaveState, new HashSet<string>(), null, null);
            Box card = cardData.DrawSaveableEntityCard(null);
            windowHelper = new HeadlessEditorWindowTestHelper();
            windowHelper.Attach(card);
            Toggle toggle = card.Query<Toggle>().First();
            toggle.value = true; // simulate an edited-but-unsaved sub-card value through the real UI field

            cardData.SaveSaveableEntity(saveCachedStateToFile: false);

            Assert.IsTrue(cachedFullSaveState.ContainsKey(entity.GetUniqueIdentifier()));
            Assert.IsFalse(File.Exists(Path.Combine(Application.persistentDataPath, _tempSaveFile + ".sav")));
        }

        [Test]
        public void SaveSaveableEntity_WithFileWrite_WritesToDisk()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestBoolSaveableComponent>();

            var cachedFullSaveState = new JObject();
            var cardData = new SaveableEntityCardData(entity, cachedFullSaveState, new HashSet<string>(), null, () => _tempSaveFile);
            Box card = cardData.DrawSaveableEntityCard(null);
            windowHelper = new HeadlessEditorWindowTestHelper();
            windowHelper.Attach(card);
            Toggle toggle = card.Query<Toggle>().First();
            toggle.value = true;

            cardData.SaveSaveableEntity(saveCachedStateToFile: true);

            JObject savedState = SavingSystem.ManualGetFullState(_tempSaveFile);
            Assert.IsTrue(savedState.ContainsKey(entity.GetUniqueIdentifier()));
        }

        [Test]
        public void SaveSaveableEntity_PlayerMoverDesynced_InvokesPositionChangeCallback()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestPlayerMoverSaveableComponent>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);
            cardData.TryGetSaveableSubCardData(out PlayerMoverMarkerSubCard subCard);
            subCard.TestTriggerStateChanged(); // desyncs it, marking the player as having moved

            bool callbackInvoked = false;
            cardData.SaveSaveableEntity(saveCachedStateToFile: false, () => callbackInvoked = true);

            Assert.IsTrue(callbackInvoked);
        }

        [Test]
        public void SaveSaveableEntity_NoPlayerMoverDesynced_DoesNotInvokePositionChangeCallback()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestPlayerMoverSaveableComponent>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);

            bool callbackInvoked = false;
            cardData.SaveSaveableEntity(saveCachedStateToFile: false, () => callbackInvoked = true);

            Assert.IsFalse(callbackInvoked);
        }
        #endregion

        #region DrawSaveableEntityCard
        [Test]
        public void DrawSaveableEntityCard_ContainsExpectedLabelsAndButtons()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();
            gameObject.AddComponent<TestSaveableComponent>().captureStateReturns = "value";

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);
            Box card = cardData.DrawSaveableEntityCard(null);

            List<string> labels = card.Query<Label>().ToList().Select(label => label.text).ToList();
            CollectionAssert.Contains(labels, $"ID:  {cardData.entityID}");
            Assert.IsTrue(labels.Any(text => text.StartsWith("Component:  ")));

            List<string> buttons = card.Query<Button>().ToList().Select(button => button.text).ToList();
            CollectionAssert.Contains(buttons, "Select Entity");
            CollectionAssert.Contains(buttons, "Save Entity");
        }

        [Test]
        public void DrawSaveableEntityCard_SaveButtonClick_InvokesSaveCallback()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);
            bool saveCallbackInvoked = false;
            Box card = cardData.DrawSaveableEntityCard(() => saveCallbackInvoked = true);
            windowHelper = new HeadlessEditorWindowTestHelper();
            windowHelper.Attach(card);

            Button saveButton = card.Query<Button>().ToList().First(button => button.text == "Save Entity");
            using (ClickEvent clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = saveButton;
                saveButton.SendEvent(clickEvent);
            }

            Assert.IsTrue(saveCallbackInvoked);
        }

        [Test]
        public void SetIsDataSynced_False_ChangesCardBackgroundColor()
        {
            GameObject gameObject = NewGameObject("Entity");
            var entity = gameObject.AddComponent<SaveableEntity>();

            var cardData = new SaveableEntityCardData(entity, new JObject(), new HashSet<string>(), null, null);
            Box card = cardData.DrawSaveableEntityCard(null);
            StyleColor colorWhenSynced = card.style.backgroundColor;

            cardData.SetIsDataSynced(false);

            Assert.AreNotEqual(colorWhenSynced, card.style.backgroundColor);
        }
        #endregion
    }
}
