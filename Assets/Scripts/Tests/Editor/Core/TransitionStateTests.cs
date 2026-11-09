using NUnit.Framework;
using Frankie.Core;
using Frankie.Core.PlayerStates;

namespace Frankie.Tests.Editor
{
    public class TransitionStateTests
    {
        // Covers TransitionState's combat handling against FakePlayerStateContext
        //  - Combat requests act only during a battle entry (fade complete -> commit, else swarm), and are dropped otherwise
        //  - Dialogue / cutscene requests queue during a battle entry only, World holds until a zone transition completes
        // Not covered here: fade / swarm chain / scene load wiring

        // State
        private TransitionState transitionState;
        private FakePlayerStateContext context;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            transitionState = new TransitionState();
            context = new FakePlayerStateContext();
        }
        #endregion

        #region Combat
        [Test]
        public void EnterCombat_BattleEntryFadeComplete_CommitsCombatState()
        {
            context.inBattleEntryTransition = true;
            context.combatFadeComplete = true;

            transitionState.EnterCombat(context);

            Assert.AreSame(PlayerStateMachine.combatState, context.committedState);
        }

        [Test]
        public void EnterCombat_BattleEntryFadeRunning_AddsEnemiesToTheSwarm()
        {
            context.inBattleEntryTransition = true;
            context.combatParticipantsValid = true;

            transitionState.EnterCombat(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.ConfirmEnemiesUnderConsideration) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterCombat_NotBattleEntryWithStaleFadeComplete_IsDropped()
        {
            // e.g. a zone transition started from Combat, before combat memory resets on the return to World
            context.combatFadeComplete = true;
            context.combatParticipantsValid = true;

            transitionState.EnterCombat(context);

            CollectionAssert.IsEmpty(context.calls);
            Assert.IsNull(context.committedState);
        }
        #endregion

        #region DialogueCutScene
        [Test]
        public void EnterDialogue_BattleEntry_IsQueued()
        {
            context.inBattleEntryTransition = true;

            transitionState.EnterDialogue(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.QueueActionUnderConsideration) }, context.calls);
        }

        [Test]
        public void EnterDialogue_ZoneTransition_IsDropped()
        {
            context.inZoneTransition = true;

            transitionState.EnterDialogue(context);

            CollectionAssert.IsEmpty(context.calls);
        }

        [Test]
        public void EnterCutScene_BattleEntry_IsQueued()
        {
            context.inBattleEntryTransition = true;

            transitionState.EnterCutScene(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.QueueActionUnderConsideration) }, context.calls);
        }

        [Test]
        public void EnterCutScene_ZoneTransition_IsDropped()
        {
            context.inZoneTransition = true;

            transitionState.EnterCutScene(context);

            CollectionAssert.IsEmpty(context.calls);
        }
        #endregion

        #region World
        [Test]
        public void EnterWorld_ZoneTransitionRunning_HoldsInTransition()
        {
            context.inZoneTransition = true;
            context.zoneTransitionComplete = false;

            transitionState.EnterWorld(context);

            CollectionAssert.IsEmpty(context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterWorld_ZoneTransitionComplete_ClearsMemoryAndCommitsWorld()
        {
            context.inZoneTransition = true;

            transitionState.EnterWorld(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.ClearPlayerStateMemory), nameof(IPlayerStateContext.SetPlayerState) }, context.calls);
            Assert.AreSame(PlayerStateMachine.worldState, context.committedState);
        }

        [Test]
        public void EnterWorld_BattleTransition_CommitsWorld()
        {
            context.zoneTransitionComplete = false; // Ignored outside a zone transition

            transitionState.EnterWorld(context);

            Assert.AreSame(PlayerStateMachine.worldState, context.committedState);
        }
        #endregion
    }
}
