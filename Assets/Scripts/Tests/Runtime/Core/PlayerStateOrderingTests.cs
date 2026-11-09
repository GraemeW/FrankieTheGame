#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Core;
using Frankie.Speech;

namespace Frankie.Tests
{
    public class PlayerStateOrderingTests
    {
        // Covers listener ordering when a playerStateChanged listener requests a new state (Player's game-over listener pattern)
        // Stand-in listener enters a cutscene from inside the InWorld event, rather than killing the party (that queues the GameOver scene)

        // State
        private readonly TestObjectTracker objects = new();
        private PlayerStateRig rig;
        private readonly List<PlayerStateType> lateListenerStates = new();
        private Action<PlayerStateType, IPlayerStateInfo> cutsceneOnWorld;
        private Action<PlayerStateType, IPlayerStateInfo> lateListener;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PlayerStateRig();
            yield return rig.Build();
            lateListenerStates.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (rig.playerStateMachine != null)
            {
                rig.playerStateMachine.playerStateChanged -= cutsceneOnWorld;
                rig.playerStateMachine.playerStateChanged -= lateListener;
            }
            yield return rig.TearDown();
            objects.DestroyAll();
        }
        #endregion
        
        #region Helpers
        private void SubscribeCutsceneOnFirstWorld()
        {
            var hasFired = false;
            cutsceneOnWorld = (playerStateType, _) =>
            {
                if (hasFired || playerStateType != PlayerStateType.InWorld) { return; }
                hasFired = true;
                rig.playerStateMachine.EnterCutscene(true, false);
            };
            lateListener = (playerStateType, _) => lateListenerStates.Add(playerStateType);

            rig.playerStateMachine.playerStateChanged += cutsceneOnWorld;
            rig.playerStateMachine.playerStateChanged += lateListener;
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator WorldListenerEnteringCutscene_EveryListenerSeesWorldThenCutscene()
        {
            rig.playerStateMachine.EnterCutscene();
            SubscribeCutsceneOnFirstWorld();
            rig.observedStates.Clear();

            rig.playerStateMachine.EnterWorld();

            Assert.AreEqual(PlayerStateType.InCutScene, rig.GetState());
            CollectionAssert.AreEqual(new[] { PlayerStateType.InWorld, PlayerStateType.InCutScene }, rig.observedStates, "Listener before the requester");
            CollectionAssert.AreEqual(new[] { PlayerStateType.InWorld, PlayerStateType.InCutScene }, lateListenerStates, "Listener after the requester");
            yield return null;
        }

        [UnityTest]
        public IEnumerator WorldListenerEnteringCutscene_QueuedActionDoesNotPopDuringCutscene()
        {
            AIConversant conversant = rig.SpawnConversant("TestNPC");
            rig.playerStateMachine.EnterCutscene();
            rig.playerStateMachine.EnterDialogue(conversant, objects.CreateSingleLineDialogue()); // Queued behind the cutscene
            SubscribeCutsceneOnFirstWorld();

            rig.playerStateMachine.EnterWorld();
            yield return PlayerStateRig.Settle(10);

            Assert.AreEqual(PlayerStateType.InCutScene, rig.GetState(), "Queue popped during the listener's cutscene");
            Assert.IsNull(DialogueController.FindDialogueController());

            rig.playerStateMachine.EnterWorld();
            yield return TestWait.Until(() => rig.GetState() == PlayerStateType.InDialogue, PlayerStateRig.waitTimeoutSeconds);
            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState(), "Queued dialogue lost");
        }
        #endregion
    }
}
#endif
