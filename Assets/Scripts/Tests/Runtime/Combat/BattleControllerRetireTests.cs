#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Frankie.Combat;
using Frankie.Zones;

namespace Frankie.Tests
{
    public class BattleControllerRetireTests
    {
        // Covers BattleController's ExitComplete retire (deactivate before destroy) + AbortBattle teardown / Retire against the prefab + Player
        // Drives the battle event bus directly rather than a full fight -- i.e. the fade / combat flow itself is not covered here

        // Const (note that paths are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _battleControllerPrefabPath = "Assets/Game/Controllers/Battle Controller.prefab";

        // State
        private PlayerStateRig rig;
        private int battleSetUpCount;
        private int battleTornDownCount;
        private readonly List<BattleState> observedBattleStates = new();
        private BattleOutcome lastBattleOutcome;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PlayerStateRig();
            yield return rig.Build();
            battleSetUpCount = 0;
            battleTornDownCount = 0;
            observedBattleStates.Clear();
            BattleEventBus<BattleStagingEvent>.SubscribeToEvent(CountBattleSetUp);
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(RecordBattleState);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            BattleEventBus<BattleStagingEvent>.UnsubscribeFromEvent(CountBattleSetUp);
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(RecordBattleState);
            yield return rig.TearDown();
        }
        #endregion
        
        #region Helpers
        private BattleController SpawnBattleController()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_battleControllerPrefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at '{_battleControllerPrefabPath}'");
            return Object.Instantiate(prefab).GetComponent<BattleController>();
        }

        private void CountBattleSetUp(BattleStagingEvent battleStagingEvent)
        {
            if (battleStagingEvent.battleStagingType == BattleStagingType.BattleSetUp) { battleSetUpCount++; }
            if (battleStagingEvent.battleStagingType == BattleStagingType.BattleTornDown) { battleTornDownCount++; }
        }

        private void RecordBattleState(BattleStateChangedEvent battleStateChangedEvent)
        {
            observedBattleStates.Add(battleStateChangedEvent.battleState);
            lastBattleOutcome = battleStateChangedEvent.battleOutcome;
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator ExitComplete_ControllerRequestedSameFrame_IsFreshAndReceivesEntryPeak()
        {
            BattleController retiringController = SpawnBattleController();
            yield return null;

            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.ExitComplete));

            Assert.IsFalse(retiringController.gameObject.activeSelf, "Retiring controller should deactivate immediately");
            Assert.IsNull(BattleController.FindBattleController(), "Tag lookup still finds the retiring controller");

            BattleController freshController = SpawnBattleController(); // Same frame as the retire
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.EntryPeak, new List<CombatParticipant>(), TransitionType.BattleNeutral));
            yield return null;

            Assert.IsTrue(retiringController == null, "Retiring controller should be destroyed");
            Assert.IsTrue(freshController != null, "Fresh controller destroyed itself (VerifyUnique)");
            Assert.AreSame(freshController, BattleController.FindBattleController());
            Assert.AreEqual(1, battleSetUpCount, "EntryPeak should reach only the fresh controller");
        }

        [UnityTest]
        public IEnumerator AbortBattle_MidCombat_CompletesAndTearsDownWithoutOutro_StaysInertUntilRetired()
        {
            BattleController battleController = SpawnBattleController();
            yield return null;
            BattleEventBus<BattleFadeTransitionEvent>.Raise(new BattleFadeTransitionEvent(BattleFadePhase.EntryPeak, new List<CombatParticipant>(), TransitionType.BattleNeutral));
            battleController.SetBattleState(BattleState.Combat, BattleOutcome.Undetermined);
            Assert.IsTrue(BattleEventBus.inBattle);

            battleController.AbortBattle();

            CollectionAssert.AreEqual(new[] { BattleState.Combat, BattleState.Complete }, observedBattleStates, "Abort should skip Outro / Rewards");
            Assert.AreEqual(BattleOutcome.Aborted, lastBattleOutcome);
            Assert.AreEqual(1, battleTornDownCount);
            Assert.IsFalse(BattleEventBus.inBattle);
            Assert.AreEqual(BattleState.Inactive, BattleEventBus.battleState, "BattleTornDown resets the bus, as on a normal battle end");

            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(RecordBattleState); // BattleTornDown clears within-battle subscriptions
            yield return null;
            Assert.IsTrue(battleController != null && battleController.gameObject.activeSelf, "Aborted controller should stay up (inert) until retired");
            CollectionAssert.AreEqual(new[] { BattleState.Combat, BattleState.Complete }, observedBattleStates, "Inert controller should raise nothing further");

            battleController.Retire();
            Assert.IsFalse(battleController.gameObject.activeSelf, "Retired controller should deactivate immediately");
            Assert.IsNull(BattleController.FindBattleController());

            yield return null;
            Assert.IsTrue(battleController == null, "Retired controller should be destroyed");
        }
        #endregion
    }
}
#endif
