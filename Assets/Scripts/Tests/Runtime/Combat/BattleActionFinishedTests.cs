#if UNITY_EDITOR
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;
using Unity.Scripting.LifecycleManagement;

namespace Frankie.Tests
{
    public class BattleActionFinishedTests
    {
        // Tested:
        //  - CombatParticipant.StartBattleActionCoroutine - finished fires once on completion;  once when stopped by disable / a replacing action;  a callback starting another action doesn't re-fire it
        //  - BattleAction.Use - finished fires once after its effects;  an interrupted sequence stops its effects + skips its cooldown (no CooldownSet)
        //  - Paused (queued) cooldown release - an in-combat interrupt or an inactive sender releases it;  a replacing action keeps it
        //  - BattleAction.Use on an inactive sender - returns false, fires finished, runs no effects
        // Not Tested:
        //  - The battle-aborted stop path - needs a staged BattleStateChangedEvent;  goes through the same StopBattleActionCoroutine as disable
        //  - BattleController's queue resuming - covered by the callback firing (it only clears battleSequenceInProgress)
        // Notes:
        //  - Play Mode, since the action runs as a coroutine on the participant

        // Const
        private const string _actionName = "BattleActionFinishedTests_Action";
        private const string _effectName = "BattleActionFinishedTests_Effect";
        [NoAutoStaticsCleanup] private static readonly Regex _droppedWarning = new("Battle action for .* dropped");

        // State
        private readonly TestObjectTracker tracker = new();
        private CombatParticipant sender;
        private int finishedCount;
        private int cooldownSetCount;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            sender = tracker.CreateComponent<CombatParticipant>("BattleActionFinishedSender"); // Pulls in BaseStats
            finishedCount = 0;
            cooldownSetCount = 0;
            sender.stateAltered += CountCooldownSet;
        }

        [TearDown]
        public void TearDown()
        {
            if (sender != null) { sender.stateAltered -= CountCooldownSet; }
            sender.LeaveCombat();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private void CountFinished() => finishedCount++;

        private void CountCooldownSet(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo.stateAlteredType == StateAlteredType.CooldownSet) { cooldownSetCount++; }
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator StartBattleActionCoroutine_Completes_FiresFinishedOnce()
        {
            sender.StartBattleActionCoroutine(TestWait.Frames(2), CountFinished);
            Assert.AreEqual(0, finishedCount);

            yield return TestWait.Frames(4);
            Assert.AreEqual(1, finishedCount);
        }

        [UnityTest]
        public IEnumerator StartBattleActionCoroutine_SenderDisabledMidAction_FiresFinishedOnce()
        {
            sender.StartBattleActionCoroutine(TestWait.Frames(10), CountFinished);
            yield return null;

            sender.gameObject.SetActive(false);
            Assert.AreEqual(1, finishedCount);

            sender.gameObject.SetActive(true);
            yield return TestWait.Frames(12);
            Assert.AreEqual(1, finishedCount);
        }

        [UnityTest]
        public IEnumerator StartBattleActionCoroutine_ReplacedByAnotherAction_FiresEachFinishedOnce()
        {
            int secondFinishedCount = 0;
            sender.StartBattleActionCoroutine(TestWait.Frames(10), CountFinished);
            yield return null;

            sender.StartBattleActionCoroutine(TestWait.Frames(1), () => secondFinishedCount++);
            Assert.AreEqual(1, finishedCount);

            yield return TestWait.Frames(3);
            Assert.AreEqual(1, finishedCount);
            Assert.AreEqual(1, secondFinishedCount);
        }

        [UnityTest]
        public IEnumerator StartBattleActionCoroutine_FinishedStartsAnotherAction_DoesNotRefire()
        {
            int secondFinishedCount = 0;
            sender.StartBattleActionCoroutine(TestWait.Frames(1), () =>
            {
                finishedCount++;
                sender.StartBattleActionCoroutine(TestWait.Frames(1), () => secondFinishedCount++);
            });

            yield return TestWait.Frames(5);
            Assert.AreEqual(1, finishedCount);
            Assert.AreEqual(1, secondFinishedCount);
        }

        [UnityTest]
        public IEnumerator Use_EffectsComplete_FiresFinishedOnceAfterEffects()
        {
            TestYieldingEffect effect = tracker.CreateYieldingEffect(_effectName, 2);
            BattleAction battleAction = tracker.CreateBattleAction(_actionName, effect);

            Assert.IsTrue(battleAction.Use(CombatTestBuilders.CreateSelfTargetedActionData(sender), false, CountFinished));
            Assert.AreEqual(0, finishedCount);

            yield return TestWait.Frames(5);
            Assert.AreEqual(1, effect.completedCount);
            Assert.AreEqual(1, finishedCount);
            Assert.AreEqual(1, cooldownSetCount);
        }

        [UnityTest]
        public IEnumerator Use_SenderDisabledMidEffect_FiresFinishedAndStopsTheSequence()
        {
            TestYieldingEffect firstEffect = tracker.CreateYieldingEffect(_effectName, 5);
            TestYieldingEffect secondEffect = tracker.CreateYieldingEffect(_effectName, 1);
            BattleAction battleAction = tracker.CreateBattleAction(_actionName, firstEffect, secondEffect);

            Assert.IsTrue(battleAction.Use(CombatTestBuilders.CreateSelfTargetedActionData(sender), false, CountFinished));
            yield return null;
            sender.gameObject.SetActive(false);
            Assert.AreEqual(1, finishedCount);

            sender.gameObject.SetActive(true);
            yield return TestWait.Frames(10);
            Assert.AreEqual(1, finishedCount);
            Assert.AreEqual(0, firstEffect.completedCount);
            Assert.AreEqual(0, secondEffect.startedCount);
            Assert.AreEqual(0, cooldownSetCount);
        }

        [UnityTest]
        public IEnumerator Use_SenderDisabledMidEffectInCombat_ReleasesPausedCooldown()
        {
            sender.EnterCombat();
            sender.PauseCooldown(); // As BattleController does on queueing
            BattleAction battleAction = tracker.CreateBattleAction(_actionName, tracker.CreateYieldingEffect(_effectName, 5));

            Assert.IsTrue(battleAction.Use(CombatTestBuilders.CreateSelfTargetedActionData(sender), false, CountFinished));
            yield return null;
            Assert.IsTrue(sender.IsInCooldown());

            sender.gameObject.SetActive(false);
            Assert.AreEqual(1, finishedCount);
            Assert.IsFalse(sender.IsInCooldown());
        }

        [UnityTest]
        public IEnumerator StartBattleActionCoroutine_ReplacedInCombat_KeepsPausedCooldown()
        {
            sender.EnterCombat();
            sender.StartBattleActionCoroutine(TestWait.Frames(10), CountFinished);
            yield return null;
            sender.PauseCooldown();

            sender.StartBattleActionCoroutine(TestWait.Frames(10), null);
            Assert.AreEqual(1, finishedCount);
            Assert.IsTrue(sender.IsInCooldown());
        }

        [Test]
        public void Use_SenderInactiveInCombat_ReturnsFalseFiresFinishedAndReleasesPausedCooldown()
        {
            sender.EnterCombat();
            sender.PauseCooldown();
            TestYieldingEffect effect = tracker.CreateYieldingEffect(_effectName, 1);
            BattleAction battleAction = tracker.CreateBattleAction(_actionName, effect);
            sender.gameObject.SetActive(false);

            LogAssert.Expect(LogType.Warning, _droppedWarning);
            Assert.IsFalse(battleAction.Use(CombatTestBuilders.CreateSelfTargetedActionData(sender), false, CountFinished));
            Assert.AreEqual(1, finishedCount);
            Assert.AreEqual(0, effect.startedCount);
            Assert.IsFalse(sender.IsInCooldown());
        }
        #endregion
    }
}
#endif
