using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests
{
    public class TextScanBoxTests
    {
        // Test Notes:
        //  - the box's view (UIToolkitTextScanView) has no UI loaded here, so it never binds
        //  - entries are then handles + models with no elements ~ i.e. the surface TextScanBox drives (reveal, text, removal)
        
        // State
        private readonly List<GameObject> spawned = new();

        // Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned.Where(go => go != null))
            {
                Object.Destroy(go);
            }
            spawned.Clear();
        }

        #region PrivateMethods
        private TestTextScanBox CreateInactiveTextScanBox(float delayBetweenCharacters = 0f, float initialInputDelay = 0f)
        {
            var go = new GameObject("TextScanBox");
            spawned.Add(go);
            go.SetActive(false);
            var textScanBox = go.AddComponent<TestTextScanBox>();
            textScanBox.initialInputDelay = initialInputDelay;
            textScanBox.delayBetweenCharacters = delayBetweenCharacters;

            var controllerGo = new GameObject("Controller");
            spawned.Add(controllerGo);
            textScanBox.TrySetController(controllerGo.AddComponent<TestBaseController>());

            return textScanBox;
        }
        #endregion

        #region Setup
        [UnityTest]
        public IEnumerator Awake_ClearsChoiceOptionsOnEnable()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;

            // Choices belong to the text they were created with, so they do not persist across enables
            Assert.IsTrue(textScanBox.publicClearChoiceOptionsOnEnable);
        }
        #endregion

        #region ClearOldDialogue
        [UnityTest]
        public IEnumerator ClearOldDialogue_RemovesPrintedEntries()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;
            TextEntryHandle printedEntry = textScanBox.PublicAddTextEntry("Hi");
            for (int frame = 0; frame < 20 && textScanBox.printedJobs.Count == 0; frame++)
            {
                yield return null;
            }
            CollectionAssert.Contains(textScanBox.printedJobs, printedEntry);

            textScanBox.ClearOldDialogue();

            Assert.IsFalse(printedEntry.isAlive);
            CollectionAssert.IsEmpty(textScanBox.printedJobs);
        }

        [UnityTest]
        public IEnumerator ClearOldDialogue_RemovesEntriesStillWaitingToPrint()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;
            TextEntryHandle pendingText = textScanBox.PublicAddTextEntry("Hello World");
            ChoiceEntryHandle pendingChoice = textScanBox.PublicAddChoiceEntry("Choice");

            textScanBox.ClearOldDialogue();

            Assert.IsFalse(pendingText.isAlive);
            Assert.IsFalse(pendingChoice.isAlive);
        }
        #endregion

        #region Typewriter
        [UnityTest]
        public IEnumerator AddText_RevealsOneCharacterPerFrame_ThenCompletesWithFullText()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;

            TextEntryHandle textEntry = textScanBox.PublicAddTextEntry("Hi");
            Assert.IsFalse(textEntry.isRevealed); // Entries wait their turn hidden
            yield return null; // Update() dequeues and starts the coroutine, which runs to its first yield

            Assert.IsTrue(textScanBox.publicIsWriting);
            Assert.IsTrue(textEntry.isRevealed);
            Assert.AreEqual("H", textEntry.text);

            yield return null; // delayBetweenCharacters is 0, so the coroutine finishes on the next frame

            Assert.IsFalse(textScanBox.publicIsWriting);
            Assert.AreEqual("Hi", textEntry.text);
        }

        [UnityTest]
        public IEnumerator AddText_FinalRevealUsesUnescapedFullText()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;

            const string raw = "line1\\nline2"; // 12 chars raw, 11 chars once \n is unescaped
            const string unescaped = "line1\nline2";
            TextEntryHandle textEntry = textScanBox.PublicAddTextEntry(raw);
            yield return null; // starts the coroutine this frame
            for (int frame = 0; frame < 20 && textScanBox.publicIsWriting; frame++)
            {
                yield return null;
            }

            Assert.IsFalse(textScanBox.publicIsWriting);
            Assert.AreEqual(unescaped, textEntry.text); // NOT the raw "line1\\nline2"
        }

        [UnityTest]
        public IEnumerator AddText_EntryRemovedBeforeItsTurn_NeverSetsIsWriting()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;

            TextEntryHandle textEntry = textScanBox.PublicAddTextEntry("Hello");
            textEntry.Remove();
            yield return null;
            yield return null;

            // The dead-entry check runs before SetBusyWriting(true)
            // So isWriting is never set true for a removed entry && never gets stuck
            Assert.IsFalse(textScanBox.publicIsWriting);
        }

        [UnityTest]
        public IEnumerator SkipToEndOfPage_WhileWriting_JumpsStraightToFinalText()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;

            TextEntryHandle textEntry = textScanBox.PublicAddTextEntry("Hello World");
            yield return null; // first character ('H') is shown, coroutine is now mid-loop

            bool wasWriting = textScanBox.PublicTryFastForwardActiveText();
            Assert.IsTrue(wasWriting);

            yield return null; // loop resumes, sees interruptWriting, breaks, reveals full text

            Assert.IsFalse(textScanBox.publicIsWriting);
            Assert.AreEqual("Hello World", textEntry.text);
        }

        [Test]
        public void TryFastForwardActiveText_NotCurrentlyWriting_ReturnsFalse()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();

            Assert.IsFalse(textScanBox.PublicTryFastForwardActiveText());
        }
        #endregion

        #region Choices
        [UnityTest]
        public IEnumerator QueuedChoice_RevealedOnlyAfterTheTextBeforeIt()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;

            textScanBox.PublicAddTextEntry("Hi");
            ChoiceEntryHandle choiceEntry = textScanBox.PublicAddChoiceEntry("Choice");
            yield return null; // text is mid-typewriter

            Assert.IsFalse(choiceEntry.isRevealed);

            for (int frame = 0; frame < 20 && !choiceEntry.isRevealed; frame++)
            {
                yield return null;
            }

            Assert.IsTrue(choiceEntry.isRevealed);
            Assert.IsFalse(textScanBox.publicIsWriting);
        }
        #endregion

        #region PageBreak
        [UnityTest]
        public IEnumerator AddPageBreak_SetsBusyWriting_ThenSkipClearsItImmediately()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;

            textScanBox.AddPageBreak();
            yield return null; // PrintPageBreak runs to completion in a single frame (yield break)

            Assert.IsTrue(textScanBox.publicIsWriting);

            textScanBox.PublicSkipToEndOfPage(); // queuePageClear is true, so this resolves synchronously

            Assert.IsFalse(textScanBox.publicIsWriting);
        }

        [UnityTest]
        public IEnumerator AddPageBreak_FiresWritingStateChangedTwice()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;
            int writingStateChangedCount = 0;
            textScanBox.SubscribeToReceiverUpdates(true, (type, _) =>
            {
                if (type == ReceiverModifiedType.WritingStateChanged) { writingStateChangedCount++; }
            });

            textScanBox.AddPageBreak();
            yield return null;

            // SetBusyWriting(true) fires it once
            // PrintPageBreak then explicitly overrides with a second one reporting false
            Assert.AreEqual(2, writingStateChangedCount);
        }
        #endregion

        #region InitialInputBlocking
        [UnityTest]
        public IEnumerator IsInitialInputBlocked_ClearsAfterInitialInputDelayElapses()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox(initialInputDelay: 0.05f);
            textScanBox.gameObject.SetActive(true);

            for (int frame = 0; frame < 120 && textScanBox.publicIsInitialInputBlocked; frame++)
            {
                yield return null;
            }

            Assert.IsFalse(textScanBox.publicIsInitialInputBlocked);
        }
        #endregion

        #region StateLookupDispatch
        [UnityTest]
        public IEnumerator PrepareChooseAction_Execute_FastForwardsActiveText()
        {
            // Exercises the REAL BuildStateBehaviours() registration (ImplementPrepareChooseAction)
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;
            textScanBox.AddText("Hello World");
            yield return null; // first character shown, still writing

            bool result = textScanBox.PublicPrepareChooseAction(ControllerInputType.Execute);

            Assert.IsTrue(result);
            yield return null; // let the fast-forward actually land
            Assert.IsFalse(textScanBox.publicIsWriting);
        }

        [UnityTest]
        public IEnumerator HandleGlobalInput_InitialInputStillBlocked_ReturnsFalse()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox(initialInputDelay: 999f);
            textScanBox.gameObject.SetActive(true);
            yield return null;

            // ImplementHandleGlobalInput returns false while isInitialInputBlocked, regardless of input
            // Observed indirectly via destroyQueued never getting set for Cancel
            textScanBox.GetInputHandler().Invoke(ControllerInputType.Cancel);

            Assert.IsFalse(textScanBox.destroyQueued);
        }
        #endregion

        #region DisableTriggered
        [UnityTest]
        public IEnumerator DisableTriggered_StopsActiveCoroutineAndClearsBusyWritingAndDialogue()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;
            TextEntryHandle textEntry = textScanBox.PublicAddTextEntry("Hello World");
            yield return null; // now mid-typewriter, activeTextScan is non-null

            Assert.IsTrue(textScanBox.publicIsWriting);

            textScanBox.gameObject.SetActive(false); // OnDisable -> DisableTriggered, reliable in Play Mode

            Assert.IsFalse(textScanBox.publicIsWriting);
            Assert.IsFalse(textEntry.isAlive);
        }
        #endregion
    }
}
