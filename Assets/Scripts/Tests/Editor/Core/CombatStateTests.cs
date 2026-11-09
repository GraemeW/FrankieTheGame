using NUnit.Framework;
using Frankie.Core;
using Frankie.Core.PlayerStates;

namespace Frankie.Tests.Editor
{
    public class CombatStateTests
    {
        // Covers CombatState against FakePlayerStateContext
        //  - Combat requests ignored (post-combat immunity), battle exit / zone transition (aborts the battle) handling, end-battle fail-safe
        // Not covered here: exit fade / BattleController wiring

        // State
        private CombatState combatState;
        private FakePlayerStateContext context;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            combatState = new CombatState();
            context = new FakePlayerStateContext();
        }
        #endregion

        #region Tests
        [Test]
        public void EnterCombat_IsIgnored()
        {
            context.combatParticipantsValid = true;

            combatState.EnterCombat(context);

            CollectionAssert.IsEmpty(context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterDialogue_IsQueued()
        {
            combatState.EnterDialogue(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.QueueActionUnderConsideration) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterTransition_BattleExit_CommitsTransitionThenEndsBattle()
        {
            context.inBattleExitTransition = true;

            combatState.EnterTransition(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.SetPlayerState), nameof(IPlayerStateContext.EndBattleSequence) }, context.calls);
            Assert.AreSame(PlayerStateMachine.transitionState, context.committedState);
        }

        [Test]
        public void EnterTransition_BattleExitFailsToEnd_FallsBackToWorld()
        {
            context.inBattleExitTransition = true;
            context.endBattleResult = false;

            combatState.EnterTransition(context);

            Assert.AreSame(PlayerStateMachine.worldState, context.committedState);
            CollectionAssert.Contains(context.calls, nameof(IPlayerStateContext.ClearPlayerStateMemory));
        }

        [Test]
        public void EnterTransition_ZoneTransition_AbortsBattleThenCommitsTransition()
        {
            context.inZoneTransition = true;

            combatState.EnterTransition(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.AbortBattle), nameof(IPlayerStateContext.SetPlayerState) }, context.calls);
            Assert.AreSame(PlayerStateMachine.transitionState, context.committedState);
        }

        [Test]
        public void EnterTransition_NeitherBattleExitNorZone_IsIgnored()
        {
            combatState.EnterTransition(context);

            CollectionAssert.IsEmpty(context.calls);
            Assert.IsNull(context.committedState);
        }
        #endregion
    }
}
