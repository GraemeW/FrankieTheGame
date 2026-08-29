using LowDefMustard.Saving.Editor;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine.UIElements;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Note: Tests that only read back constructed state build the Box directly with no panel
    // Note: Tests that need a RegisterValueChangedCallback to fire attach the Box to a HeadlessEditorWindowTestHelper
    //  - unattached BaseField<T>.value assignment sets the value but does not dispatch the ChangeEvent (callback silently fails to fires without a real panel)
    // Note:  Registry tests register against RegistryOnlyProbe and unregister in [TearDown] via UnregisterSubCard<T>/UnregisterSubCardPriority
    //  - ensures the static sub-card factory registry is clean once this finishes
    
    public class SaveableSubCardDataTests
    {
        // State
        private HeadlessEditorWindowTestHelper windowHelper;
        private static readonly Func<ISaveableBase, bool> _registryOnlyProbePriorityMatch = probe => probe is RegistryOnlyProbe;
        
        #region DataStructures
        private class RegistryOnlyProbe : ISaveableBase
        {
            public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
            public SaveState CaptureState() => null;
            public void RestoreState(SaveState saveState) { }
        }

        private class MarkerSubCard : SaveableSubCardData
        {
            public MarkerSubCard(ISaveableBase saveable, SaveState saveState)
            {
                this.saveable = saveable;
                this.saveState = saveState;
            }

            protected override void AddEditableFieldsToSubCardView(Box subCardView) { }
        }
        #endregion

        #region Setup
        [TearDown]
        public void TearDown()
        {
            windowHelper?.Close();
            windowHelper = null;

            SaveableSubCardData.UnregisterSubCard<RegistryOnlyProbe>();
            SaveableSubCardData.UnregisterSubCardPriority(_registryOnlyProbePriorityMatch);
        }
        #endregion

        #region Static registries
        [Test]
        public void CreateTypeSpecificSubCard_NoMatchingRule_ReturnsGenericFallback()
        {
            var probe = new TestGenericSaveable<bool>();
            var result = SaveableSubCardData.CreateTypeSpecificSubCard(probe, new SaveState(LoadPriority.ObjectProperty, false));

            Assert.IsInstanceOf<GenericSaveableSubCard>(result);
        }

        [Test]
        public void GetEntitySortPriority_NoMatchingRule_ReturnsMaxValue()
        {
            var probe = new TestGenericSaveable<bool>();
            Assert.AreEqual(int.MaxValue, SaveableSubCardData.GetEntitySortPriority(probe));
        }

        [Test]
        public void RegisterSubCard_MatchingRule_UsesCustomFactory()
        {
            SaveableSubCardData.RegisterSubCard<RegistryOnlyProbe>((probe, state) => new MarkerSubCard(probe, state));

            var result = SaveableSubCardData.CreateTypeSpecificSubCard(new RegistryOnlyProbe(), new SaveState(LoadPriority.ObjectProperty, false));

            Assert.IsInstanceOf<MarkerSubCard>(result);
        }

        [Test]
        public void RegisterSubCardPriority_MatchingRule_ReturnsRegisteredPriority()
        {
            SaveableSubCardData.RegisterSubCardPriority(_registryOnlyProbePriorityMatch, 7);

            Assert.AreEqual(7, SaveableSubCardData.GetEntitySortPriority(new RegistryOnlyProbe()));
        }

        [Test]
        public void UnregisterSubCard_RemovesCustomFactory()
        {
            SaveableSubCardData.RegisterSubCard<RegistryOnlyProbe>((probe, state) => new MarkerSubCard(probe, state));
            SaveableSubCardData.UnregisterSubCard<RegistryOnlyProbe>();

            var result = SaveableSubCardData.CreateTypeSpecificSubCard(new RegistryOnlyProbe(), new SaveState(LoadPriority.ObjectProperty, false));

            Assert.IsInstanceOf<GenericSaveableSubCard>(result);
        }
        #endregion

        #region GenericSaveableSubCard
        [Test]
        public void GenericSaveableSubCard_AddsNotImplementedLabel()
        {
            var subCard = new GenericSaveableSubCard(new TestGenericSaveable<bool>(), new SaveState(LoadPriority.ObjectProperty, false));
            var outerBox = new Box();
            subCard.DrawIntoSubCardView(outerBox);

            var labels = outerBox.Query<Label>().ToList().Select(label => label.text);
            CollectionAssert.Contains(labels, "SubCardView not implemented");
        }
        #endregion

        #region SimpleBoolSaveableSubCard
        [Test]
        public void SimpleBoolSaveableSubCard_ToggleChanged_UpdatesStateAndDesyncs()
        {
            var testSaveable = new TestGenericSaveable<bool> { tryManualGetDataFromState = _ => (true, false) };
            var subCard = new SimpleBoolSaveableSubCard(testSaveable, new SaveState(LoadPriority.ObjectProperty, false));
            var outerBox = new Box();
            subCard.DrawIntoSubCardView(outerBox);
            windowHelper = new HeadlessEditorWindowTestHelper();
            windowHelper.Attach(outerBox);

            Toggle toggle = outerBox.Query<Toggle>().First();
            Assert.IsNotNull(toggle);
            Assert.IsFalse(toggle.value);
            Assert.IsTrue(subCard.IsSaveStateSynced());

            toggle.value = true;

            Assert.IsFalse(subCard.IsSaveStateSynced());
            subCard.saveState.TryGetState(out bool newValue);
            Assert.IsTrue(newValue);

            subCard.ResetSyncFlag();
            Assert.IsTrue(subCard.IsSaveStateSynced());
        }

        [Test]
        public void SimpleBoolSaveableSubCard_FailedDataRetrieval_ShowsNoDataLabel()
        {
            var testSaveable = new TestGenericSaveable<bool> { tryManualGetDataFromState = _ => (false, default) };
            var subCard = new SimpleBoolSaveableSubCard(testSaveable, new SaveState(LoadPriority.ObjectProperty, false));
            var outerBox = new Box();
            subCard.DrawIntoSubCardView(outerBox);

            Assert.IsNull(outerBox.Query<Toggle>().First());
            var labels = outerBox.Query<Label>().ToList().Select(label => label.text);
            CollectionAssert.Contains(labels, "No save data available");
        }

        [Test]
        public void SubscribeToStateChangedEvent_EnabledThenDisabled_OnlyInvokedWhileEnabled()
        {
            var testSaveable = new TestGenericSaveable<bool> { tryManualGetDataFromState = _ => (true, false) };
            var subCard = new SimpleBoolSaveableSubCard(testSaveable, new SaveState(LoadPriority.ObjectProperty, false));
            var outerBox = new Box();
            subCard.DrawIntoSubCardView(outerBox);
            windowHelper = new HeadlessEditorWindowTestHelper();
            windowHelper.Attach(outerBox);

            int callCount = 0;

            subCard.SubscribeToStateChangedEvent(true, Handler);
            outerBox.Query<Toggle>().First().value = true;
            Assert.AreEqual(1, callCount);

            subCard.SubscribeToStateChangedEvent(false, Handler);
            outerBox.Query<Toggle>().First().value = false;
            Assert.AreEqual(1, callCount);
            return;

            // Local Functions
            void Handler(string typeKey, SaveState state) => callCount++;
        }
        #endregion

        #region SimpleFloatSaveableSubCard
        [Test]
        public void SimpleFloatSaveableSubCard_FieldChanged_UpdatesStateAndDesyncs()
        {
            var testSaveable = new TestGenericSaveable<float> { tryManualGetDataFromState = _ => (true, 1.5f) };
            var subCard = new SimpleFloatSaveableSubCard(testSaveable, new SaveState(LoadPriority.ObjectProperty, 1.5f));
            var outerBox = new Box();
            subCard.DrawIntoSubCardView(outerBox);
            windowHelper = new HeadlessEditorWindowTestHelper();
            windowHelper.Attach(outerBox);

            FloatField floatField = outerBox.Query<FloatField>().First();
            Assert.AreEqual(1.5f, floatField.value);

            floatField.value = 2.5f;

            subCard.saveState.TryGetState(out float newValue);
            Assert.AreEqual(2.5f, newValue);
            Assert.IsFalse(subCard.IsSaveStateSynced());
        }
        #endregion

        #region SimpleIntSaveableSubCard
        [Test]
        public void SimpleIntSaveableSubCard_FieldChanged_UpdatesStateAndDesyncs()
        {
            var testSaveable = new TestGenericSaveable<int> { tryManualGetDataFromState = _ => (true, 3) };
            var subCard = new SimpleIntSaveableSubCard(testSaveable, new SaveState(LoadPriority.ObjectProperty, 3));
            var outerBox = new Box();
            subCard.DrawIntoSubCardView(outerBox);
            windowHelper = new HeadlessEditorWindowTestHelper();
            windowHelper.Attach(outerBox);

            IntegerField intField = outerBox.Query<IntegerField>().First();
            Assert.AreEqual(3, intField.value);

            intField.value = 9;

            subCard.saveState.TryGetState(out int newValue);
            Assert.AreEqual(9, newValue);
            Assert.IsFalse(subCard.IsSaveStateSynced());
        }
        #endregion
    }
}
