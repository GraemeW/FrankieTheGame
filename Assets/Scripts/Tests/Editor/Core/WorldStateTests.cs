using NUnit.Framework;
using Frankie.Core;
using Frankie.Core.PlayerStates;

namespace Frankie.Tests.Editor
{
    public class WorldStateTests
    {
        // Covers WorldState's dialogue / trade / option / combat / cutscene entry against FakePlayerStateContext
        //  - Start failure clears memory (or falls back to World for combat), closing controller queues + asks for a pop, success commits the state
        // Not covered here: PlayerStateMachine wiring (see the PlayMode suites)

        // State
        private WorldState worldState;
        private FakePlayerStateContext context;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            worldState = new WorldState();
            context = new FakePlayerStateContext();
        }
        #endregion

        #region Dialogue
        [Test]
        public void EnterDialogue_StartFails_ClearsMemoryAndStaysInWorld()
        {
            context.startDialogueResult = false;

            worldState.EnterDialogue(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.SetupDialogueController), nameof(IPlayerStateContext.StartDialogueSequence), nameof(IPlayerStateContext.ClearPlayerStateMemory) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterDialogue_ControllerClosing_QueuesRequestAndAsksForPop()
        {
            context.setupDialogueResult = false;

            worldState.EnterDialogue(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.SetupDialogueController), nameof(IPlayerStateContext.QueueActionUnderConsideration), nameof(IPlayerStateContext.RequestQueuePop) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterDialogue_StartSucceeds_CommitsDialogueState()
        {
            worldState.EnterDialogue(context);

            Assert.AreSame(PlayerStateMachine.dialogueState, context.committedState);
            CollectionAssert.DoesNotContain(context.calls, nameof(IPlayerStateContext.ClearPlayerStateMemory));
        }
        #endregion

        #region TradeOptions
        [Test]
        public void EnterTrade_StartFails_ClearsMemoryAndStaysInWorld()
        {
            context.startTradeResult = false;

            worldState.EnterTrade(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.StartTradeSequence), nameof(IPlayerStateContext.ClearPlayerStateMemory) }, context.calls);
            Assert.IsNull(context.committedState);
        }

        [Test]
        public void EnterTrade_StartSucceeds_CommitsTradeState()
        {
            worldState.EnterTrade(context);

            Assert.AreSame(PlayerStateMachine.tradeState, context.committedState);
        }

        [Test]
        public void EnterOptions_StartFails_ClearsMemoryAndStaysInWorld()
        {
            context.startOptionResult = false;

            worldState.EnterOptions(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.StartOptionSequence), nameof(IPlayerStateContext.ClearPlayerStateMemory) }, context.calls);
            Assert.IsNull(context.committedState);
        }
        #endregion

        #region Combat
        [Test]
        public void EnterCombat_InvalidParticipants_ReturnsToWorldWithoutBattleSetup()
        {
            worldState.EnterCombat(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.ClearPlayerStateMemory), nameof(IPlayerStateContext.SetPlayerState) }, context.calls);
            Assert.AreSame(PlayerStateMachine.worldState, context.committedState);
        }

        [Test]
        public void EnterCombat_Valid_SetsUpBattleThenCommitsTransitionAndStartsIt()
        {
            context.combatParticipantsValid = true;

            worldState.EnterCombat(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.SetupBattleController), nameof(IPlayerStateContext.ConfirmEnemiesUnderConsideration), nameof(IPlayerStateContext.ConfirmTransitionType), nameof(IPlayerStateContext.SetPlayerState), nameof(IPlayerStateContext.StartBattleSequence) }, context.calls);
            Assert.AreSame(PlayerStateMachine.transitionState, context.committedState);
        }

        [Test]
        public void EnterCombat_StartBattleFails_FallsBackToWorld()
        {
            context.combatParticipantsValid = true;
            context.startBattleResult = false;

            worldState.EnterCombat(context);

            Assert.AreSame(PlayerStateMachine.worldState, context.committedState);
            CollectionAssert.Contains(context.calls, nameof(IPlayerStateContext.ClearPlayerStateMemory));
        }
        #endregion

        #region CutScene
        [Test]
        public void EnterCutScene_TogglesVisibilityAndCommitsCutScene()
        {
            worldState.EnterCutScene(context);

            CollectionAssert.AreEqual(new[] { nameof(IPlayerStateContext.TogglePlayerVisibility), nameof(IPlayerStateContext.SetPlayerState) }, context.calls);
            Assert.AreSame(PlayerStateMachine.cutSceneState, context.committedState);
        }
        #endregion
    }
}
