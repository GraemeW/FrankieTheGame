using NUnit.Framework;
using Frankie.Core;
using Frankie.Core.PlayerStates;

namespace Frankie.Tests.Editor
{
    public class CutSceneStateTests
    {
        // Covers CutSceneState against FakePlayerStateContext
        //  - Cutscene chaining (queue + kick to World), zone-only transitions, visibility restore on exit
        // Not covered here: the queue pop itself (see ActionMemoryTests / PlayerStateOrderingTests)

        // State
        private CutSceneState cutSceneState;
        private FakePlayerStateContext context;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            cutSceneState = new CutSceneState();
            context = new FakePlayerStateContext();
        }
        #endregion

        #region Tests
        [Test]
        public void EnterCutScene_QueuesItThenReturnsToWorld()
        {
            cutSceneState.EnterCutScene(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.QueueActionUnderConsideration), nameof(IPlayerStateContext.TogglePlayerVisibility), nameof(IPlayerStateContext.ClearPlayerStateMemory), nameof(IPlayerStateContext.SetPlayerState) }, context.calls);
            Assert.AreSame(PlayerStateMachine.worldState, context.committedState);
        }

        [Test]
        public void EnterDialogue_IsQueued()
        {
            cutSceneState.EnterDialogue(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.QueueActionUnderConsideration) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterWorld_RestoresVisibilityAndCommitsWorld()
        {
            cutSceneState.EnterWorld(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.TogglePlayerVisibility), nameof(IPlayerStateContext.ClearPlayerStateMemory), nameof(IPlayerStateContext.SetPlayerState) }, context.calls);
            Assert.AreSame(PlayerStateMachine.worldState, context.committedState);
        }

        [Test]
        public void EnterTransition_ZoneTransition_RestoresVisibilityAndCommitsTransition()
        {
            context.inZoneTransition = true;

            cutSceneState.EnterTransition(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.TogglePlayerVisibility), nameof(IPlayerStateContext.SetPlayerState) }, context.calls);
            Assert.AreSame(PlayerStateMachine.transitionState, context.committedState);
        }

        [Test]
        public void EnterTransition_NotZoneTransition_IsIgnored()
        {
            cutSceneState.EnterTransition(context);

            CollectionAssert.IsEmpty(context.calls);
            Assert.IsNull(context.committedState);
        }
        #endregion
    }
}
