using NUnit.Framework;
using Frankie.Core;
using Frankie.Core.PlayerStates;

namespace Frankie.Tests.Editor
{
    public class DialogueStateTests
    {
        // Covers DialogueState against FakePlayerStateContext
        //  - Trade / cutscene / combat requests queue behind the dialogue, options ignored, exit commits World
        // Not covered here: the dialogue end -> pop flow (see DialogueStateFlowTests / ShopStateFlowTests)

        // State
        private DialogueState dialogueState;
        private FakePlayerStateContext context;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            dialogueState = new DialogueState();
            context = new FakePlayerStateContext();
        }
        #endregion

        #region Tests
        [Test]
        public void EnterTrade_IsQueued()
        {
            dialogueState.EnterTrade(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.QueueActionUnderConsideration) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterCutScene_IsQueued()
        {
            dialogueState.EnterCutScene(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.QueueActionUnderConsideration) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterCombat_IsQueued()
        {
            context.combatParticipantsValid = true;

            dialogueState.EnterCombat(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.QueueActionUnderConsideration) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterOptions_IsIgnored()
        {
            dialogueState.EnterOptions(context);

            CollectionAssert.IsEmpty(context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterWorld_ClearsMemoryAndCommitsWorld()
        {
            dialogueState.EnterWorld(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.ClearPlayerStateMemory), nameof(IPlayerStateContext.SetPlayerState) }, context.calls);
            Assert.AreSame(PlayerStateMachine.worldState, context.committedState);
        }
        #endregion
    }
}
