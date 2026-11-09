using NUnit.Framework;
using Frankie.Core;
using Frankie.Core.PlayerStates;

namespace Frankie.Tests.Editor
{
    public class TransitionStateTests
    {
        // Covers TransitionState's combat handling against FakePlayerStateContext
        //  - Combat requests act only during a battle entry (fade complete -> commit, else swarm), and are dropped otherwise
        // Not covered here: the real fade / swarm chain wiring

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

        #region Tests
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
    }
}
