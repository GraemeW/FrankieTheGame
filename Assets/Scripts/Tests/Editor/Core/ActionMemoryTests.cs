using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Frankie.Core;
using Frankie.Core.PlayerStateMemory;

namespace Frankie.Tests.Editor
{
    public class ActionMemoryTests
    {
        // Covers ActionMemory's queue directly (no state machine)
        //  - LIFO pop, null / unset guards, ready-to-pop gating, combat chaining, clear
        // Not covered here: Update -> TryPopQueue frame skip (see PlayerStateOrderingTests)

        // State
        private ActionMemory actionMemory;
        private List<string> invoked;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            actionMemory = new ActionMemory();
            invoked = new List<string>();
        }
        #endregion

        #region Helpers
        private void Queue(PlayerStateType playerStateType, string name)
        {
            actionMemory.SetActionUnderConsideration(playerStateType, () => invoked.Add(name));
            actionMemory.QueueActionUnderConsideration();
        }

        private void Pop()
        {
            IEnumerator pop = actionMemory.TryPopQueue(0);
            while (pop.MoveNext()) { }
        }
        #endregion

        #region Queue
        [Test]
        public void Pop_RunsMostRecentlyQueuedFirst()
        {
            Queue(PlayerStateType.InDialogue, "Dialogue");
            Queue(PlayerStateType.InCutScene, "Cutscene");

            Pop();
            CollectionAssert.AreEqual(new[] { "Cutscene" }, invoked);

            Pop();
            CollectionAssert.AreEqual(new[] { "Cutscene", "Dialogue" }, invoked);
        }

        [Test]
        public void Queue_NoActionUnderConsideration_PushesNothing()
        {
            actionMemory.QueueActionUnderConsideration();
            actionMemory.SetActionUnderConsideration(PlayerStateType.InDialogue, () => invoked.Add("Dialogue"));
            actionMemory.ResetActionUnderConsideration();
            actionMemory.QueueActionUnderConsideration();

            Pop();

            CollectionAssert.IsEmpty(invoked);
        }

        [Test]
        public void Queue_NullAction_PushesNothing()
        {
            actionMemory.SetActionUnderConsideration(PlayerStateType.InDialogue, null);
            actionMemory.QueueActionUnderConsideration();
            Queue(PlayerStateType.InCutScene, "Cutscene");

            Pop();
            Pop();

            CollectionAssert.AreEqual(new[] { "Cutscene" }, invoked);
        }

        [Test]
        public void ClearQueuedActions_EmptiesTheQueue()
        {
            Queue(PlayerStateType.InDialogue, "Dialogue");
            Queue(PlayerStateType.InBattle, "Combat");

            actionMemory.ClearQueuedActions();
            Pop();

            CollectionAssert.IsEmpty(invoked);
        }
        #endregion

        #region ReadyToPop
        [Test]
        public void ReadyToPop_SetOnWorld_IsConsumedByTheFirstRead()
        {
            Assert.IsFalse(actionMemory.ReadyToPopQueue());

            actionMemory.SetReadyToPop(PlayerStateType.InWorld);

            Assert.IsTrue(actionMemory.ReadyToPopQueue());
            Assert.IsFalse(actionMemory.ReadyToPopQueue());
        }

        [Test]
        public void ReadyToPop_NonWorldStateAfterWorld_ClearsIt()
        {
            actionMemory.SetReadyToPop(PlayerStateType.InWorld);
            actionMemory.SetReadyToPop(PlayerStateType.InCutScene);

            Assert.IsFalse(actionMemory.ReadyToPopQueue());
        }
        #endregion

        #region CombatChaining
        [Test]
        public void Pop_CombatOnTop_ChainsConsecutiveCombatAndStopsAtTheRest()
        {
            Queue(PlayerStateType.InDialogue, "Dialogue");
            Queue(PlayerStateType.InBattle, "Combat1");
            Queue(PlayerStateType.InBattle, "Combat2");

            Pop();

            CollectionAssert.AreEqual(new[] { "Combat2", "Combat1" }, invoked);
        }

        [Test]
        public void Pop_NonCombatOnTop_DoesNotChain()
        {
            Queue(PlayerStateType.InBattle, "Combat");
            Queue(PlayerStateType.InDialogue, "Dialogue");

            Pop();

            CollectionAssert.AreEqual(new[] { "Dialogue" }, invoked);
        }

        [Test]
        public void ChainQueuedCombatAction_StopsAtTheFirstNonCombatAction()
        {
            Queue(PlayerStateType.InBattle, "Combat1");
            Queue(PlayerStateType.InDialogue, "Dialogue");
            Queue(PlayerStateType.InBattle, "Combat2");

            actionMemory.ChainQueuedCombatAction();

            CollectionAssert.AreEqual(new[] { "Combat2" }, invoked);
        }

        [Test]
        public void ChainQueuedCombatAction_FromNonBattleState_DoesNothing()
        {
            Queue(PlayerStateType.InBattle, "Combat");

            actionMemory.ChainQueuedCombatAction(PlayerStateType.InDialogue);

            CollectionAssert.IsEmpty(invoked);
        }
        #endregion
    }
}
