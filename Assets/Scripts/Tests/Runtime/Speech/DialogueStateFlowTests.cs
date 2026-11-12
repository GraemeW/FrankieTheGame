#if UNITY_EDITOR
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using static LowDefMustard.Utils.Tests.ConditionBuilder;
using Frankie.Core;
using Frankie.Speech;
using Frankie.Speech.UI;

namespace Frankie.Tests
{
    public class DialogueStateFlowTests
    {
        // Covers dialogue entry / exit through the PlayerStateMachine + DialogueController (PlayerStateRig)
        //  - Option 4 (empty / gated start), onExitDialogue timing, dialogue <-> cutscene queueing, nested requests from dialogue triggers, closing-controller retry
        // Dialogues end by destroying the box (the box's DestroyTriggered -> EndConversation)

        // State
        private readonly TestObjectTracker tracker = new();
        private PlayerStateRig rig;
        private AIConversant conversant;
        private int exitCount;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PlayerStateRig();
            yield return rig.Build();

            conversant = rig.SpawnConversant("TestNPC");
            exitCount = 0;
            conversant.GetOnExitDialogue().AddListener(() => exitCount++);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return rig.TearDown();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private IEnumerator CloseDialogueBox()
        {
            yield return PlayerStateRig.Settle();
            var dialogueBox = Object.FindAnyObjectByType<DialogueBox>();
            Assert.IsNotNull(dialogueBox, "No dialogue box spawned");

            Object.Destroy(dialogueBox.gameObject);
            yield return TestWait.Until(() => DialogueController.FindDialogueController() == null, PlayerStateRig.waitTimeoutSeconds);
            Assert.IsNull(DialogueController.FindDialogueController());
        }
        #endregion
        
        #region Tests
        [UnityTest]
        public IEnumerator SimpleMessage_ClosingTheBox_ReturnsToWorldAndRetiresController()
        {
            Assert.AreEqual(PlayerStateType.InWorld, rig.GetState());

            rig.playerStateMachine.EnterDialogue("Rig spike message");
            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState());

            yield return CloseDialogueBox();

            Assert.IsNull(DialogueController.FindDialogueController());
            Assert.AreEqual(PlayerStateType.InWorld, rig.GetState());
        }

        [UnityTest]
        public IEnumerator EmptyDialogue_StaysInWorldWithNoControllerOrExitEvents()
        {
            rig.playerStateMachine.EnterDialogue(conversant, tracker.CreateEmptyDialogue());

            Assert.AreEqual(PlayerStateType.InWorld, rig.GetState());
            CollectionAssert.IsEmpty(rig.observedStates);
            yield return TestWait.Until(() => DialogueController.FindDialogueController() == null, PlayerStateRig.waitTimeoutSeconds);
            yield return PlayerStateRig.Settle();

            Assert.IsNull(DialogueController.FindDialogueController());
            Assert.IsNull(Object.FindAnyObjectByType<DialogueBox>());
            Assert.AreEqual(0, exitCount);
            CollectionAssert.IsEmpty(rig.observedStates);
        }

        [UnityTest]
        public IEnumerator FullyGatedDialogue_StaysInWorldWithNoControllerOrExitEvents()
        {
            var gatePredicate = tracker.Create<TestPredicate>("Gate");
            conversant.transform.parent.gameObject.AddComponent<FixedResultEvaluatorBehaviour>().result = false;
            Dialogue dialogue = tracker.CreateDialogue(new[] { tracker.CreateDialogueNode("Gated") }, new[] { tracker.CreateDialogueConditionNode("Gate", When(gatePredicate), "Gated") }, "Gate");

            rig.playerStateMachine.EnterDialogue(conversant, dialogue);

            Assert.AreEqual(PlayerStateType.InWorld, rig.GetState());
            yield return TestWait.Until(() => DialogueController.FindDialogueController() == null, PlayerStateRig.waitTimeoutSeconds);
            yield return PlayerStateRig.Settle();

            Assert.IsNull(DialogueController.FindDialogueController());
            Assert.AreEqual(0, exitCount);
            CollectionAssert.IsEmpty(rig.observedStates);
        }

        [UnityTest]
        public IEnumerator StandardDialogue_Ending_FiresExitExactlyOnce()
        {
            rig.playerStateMachine.EnterDialogue(conversant, tracker.CreateSingleLineDialogue());
            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState());
            Assert.AreEqual(0, exitCount);

            yield return CloseDialogueBox();
            yield return PlayerStateRig.Settle();

            Assert.AreEqual(PlayerStateType.InWorld, rig.GetState());
            Assert.AreEqual(1, exitCount);
        }

        [UnityTest]
        public IEnumerator DialogueRequestedDuringCutscene_PlaysAfterItAndExitsOnlyAfterItsOwnEnd()
        {
            rig.playerStateMachine.EnterCutscene();
            rig.playerStateMachine.EnterDialogue(conversant, tracker.CreateSingleLineDialogue());
            Assert.AreEqual(PlayerStateType.InCutScene, rig.GetState());

            rig.playerStateMachine.EnterWorld();
            yield return TestWait.Until(() => rig.GetState() == PlayerStateType.InDialogue, PlayerStateRig.waitTimeoutSeconds);

            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState(), "Queued dialogue did not play after the cutscene");
            Assert.AreEqual(0, exitCount, "onExitDialogue fired on the post-cutscene World, before the dialogue ran");

            yield return CloseDialogueBox();

            Assert.AreEqual(1, exitCount);
        }

        [UnityTest]
        public IEnumerator CutsceneRequestedDuringDialogue_PlaysOnlyAfterTheDialogueEnds()
        {
            rig.playerStateMachine.EnterDialogue(conversant, tracker.CreateSingleLineDialogue());
            rig.playerStateMachine.EnterCutscene();
            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState());

            yield return PlayerStateRig.Settle();
            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState(), "Queued cutscene popped while the dialogue was still open");

            yield return CloseDialogueBox();
            yield return TestWait.Until(() => rig.GetState() == PlayerStateType.InCutScene, PlayerStateRig.waitTimeoutSeconds);

            Assert.AreEqual(PlayerStateType.InCutScene, rig.GetState(), "Queued cutscene did not play after the dialogue");
            Assert.AreEqual(1, exitCount);
            CollectionAssert.AreEqual(new[] { PlayerStateType.InDialogue, PlayerStateType.InWorld, PlayerStateType.InCutScene }, rig.observedStates);
        }

        [UnityTest]
        public IEnumerator DialogueInitiatedTrigger_EnteringCutscene_IsDeferredAndDialogueStillCommits()
        {
            var cutsceneRequests = 0;
            conversant.gameObject.AddComponent<DialogueTrigger>().ConfigureTrigger(DialogueUpdateType.DialogueInitiated, playerStateMachine =>
            {
                cutsceneRequests++;
                playerStateMachine.EnterCutscene();
            });

            rig.playerStateMachine.EnterDialogue(conversant, tracker.CreateSingleLineDialogue());

            Assert.AreEqual(1, cutsceneRequests);
            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState());
            CollectionAssert.AreEqual(new[] { PlayerStateType.InDialogue }, rig.observedStates);
            DialogueController dialogueController = DialogueController.FindDialogueController();
            Assert.IsNotNull(dialogueController);
            Assert.IsTrue(dialogueController.IsActive());
            yield return null;
        }

        [UnityTest]
        public IEnumerator DialogueCompleteTrigger_OpeningMessage_AppearsOnFreshController()
        {
            conversant.gameObject.AddComponent<DialogueTrigger>().ConfigureTrigger(DialogueUpdateType.DialogueComplete, playerStateMachine => playerStateMachine.EnterDialogue("Follow-up message"));

            rig.playerStateMachine.EnterDialogue(conversant, tracker.CreateSingleLineDialogue());
            DialogueController firstController = DialogueController.FindDialogueController();
            Assert.IsNotNull(firstController);

            yield return PlayerStateRig.Settle();
            Object.Destroy(Object.FindAnyObjectByType<DialogueBox>().gameObject);
            yield return null;
            Assert.AreEqual(PlayerStateType.InWorld, rig.GetState(), "Follow-up reused the closing controller");

            yield return TestWait.Until(() => rig.GetState() == PlayerStateType.InDialogue, PlayerStateRig.waitTimeoutSeconds);

            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState(), "Follow-up message was never retried");
            Assert.IsTrue(firstController == null, "First controller should be destroyed");
            DialogueController followUpController = DialogueController.FindDialogueController();
            Assert.IsNotNull(followUpController);
            Assert.IsTrue(followUpController.IsSimpleMessage());
            Assert.AreEqual("Follow-up message", followUpController.GetSimpleMessage());
        }

        [UnityTest]
        public IEnumerator DialogueBoxLostMidDialogue_ReturnsToWorldAndFiresExitOnce()
        {
            // The controller loses its only receiver without EndConversation -> BaseController's poll recovers (its own Warning)
            rig.playerStateMachine.EnterDialogue(conversant, tracker.CreateSingleLineDialogue());
            yield return PlayerStateRig.Settle();

            LogAssert.Expect(LogType.Warning, new Regex(@"has no active input receivers\. Queuing it for destroy\.$"));
            Object.FindAnyObjectByType<DialogueBox>().gameObject.SetActive(false);
            yield return TestWait.Until(() => DialogueController.FindDialogueController() == null, PlayerStateRig.waitTimeoutSeconds);
            yield return PlayerStateRig.Settle();

            Assert.AreEqual(PlayerStateType.InWorld, rig.GetState());
            Assert.IsNull(DialogueController.FindDialogueController());
            Assert.AreEqual(1, exitCount);
        }
        #endregion
    }
}
#endif
