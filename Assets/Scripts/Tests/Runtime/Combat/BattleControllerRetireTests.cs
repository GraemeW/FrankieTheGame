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
        // Covers BattleController's ExitComplete retire (deactivate before destroy) against the prefab + Player
        // Drives the battle event bus directly rather than a full fight, so the fade / combat flow itself is not covered here

        // Const (note that paths are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _battleControllerPrefabPath = "Assets/Game/Controllers/Battle Controller.prefab";

        // State
        private PlayerStateRig rig;
        private int battleSetUpCount;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PlayerStateRig();
            yield return rig.Build();
            battleSetUpCount = 0;
            BattleEventBus<BattleStagingEvent>.SubscribeToEvent(CountBattleSetUp);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            BattleEventBus<BattleStagingEvent>.UnsubscribeFromEvent(CountBattleSetUp);
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
        #endregion
    }
}
#endif
