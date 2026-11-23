#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Speech;

namespace Frankie.Tests
{
    public class DialogueTriggerBuildCopyTests
    {
        // Tested:
        //  - DialogueTrigger node entry - a trigger set on a build copy of the entry node fires when the dialogue enters it
        //  - DialogueTrigger node entry - a trigger set on a different node doesn't fire
        // Not Tested:
        //  - DialogueBox choice highlighting / DialogueController choice navigation - both sides always come from the running dialogue
        // Note:  Entry fires synchronously in EnterDialogue (triggers are set up just before the entry node is set)

        // State
        private readonly TestObjectTracker tracker = new();
        private PlayerStateRig rig;
        private AIConversant conversant;
        private DialogueNode entryNode;
        private Dialogue dialogue;
        private int triggerCount;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PlayerStateRig();
            yield return rig.Build();

            conversant = rig.SpawnConversant("TestNPC");
            entryNode = tracker.CreateDialogueNode("Line");
            dialogue = tracker.CreateDialogue(new[] { entryNode }, new DialogueConditionNode[0], "Line");
            triggerCount = 0;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return rig.TearDown();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private void AddNodeEntryTrigger(DialogueNode triggerNode)
        {
            conversant.gameObject.AddComponent<DialogueTrigger>().ConfigureTrigger(DialogueUpdateType.DialogueNodeEntry, triggerNode, true, _ => triggerCount++);
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator NodeEntryTrigger_EntryNodeBuildCopy_Fires()
        {
            AddNodeEntryTrigger(tracker.CreateBuildCopy(entryNode));

            rig.playerStateMachine.EnterDialogue(conversant, dialogue);

            Assert.AreEqual(1, triggerCount, "Trigger on the build copy never fired ~ matched by instance");
            yield return null;
        }

        [UnityTest]
        public IEnumerator NodeEntryTrigger_DifferentNode_DoesNotFire()
        {
            AddNodeEntryTrigger(tracker.CreateDialogueNode("OtherLine"));

            rig.playerStateMachine.EnterDialogue(conversant, dialogue);

            Assert.AreEqual(0, triggerCount);
            yield return null;
        }
        #endregion
    }
}
#endif
