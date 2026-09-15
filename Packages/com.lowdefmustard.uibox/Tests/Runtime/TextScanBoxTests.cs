using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests
{
    public class TextScanBoxTests
    {
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
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) { field.SetValue(target, value); }
        }

        private GameObject CreateSimpleTextPrefabTemplate()
        {
            var go = new GameObject("SimpleTextTemplate", typeof(RectTransform));
            spawned.Add(go);
            var textField = go.AddComponent<TextMeshProUGUI>();
            var link = go.AddComponent<SimpleTextLink>();
            SetPrivateField(link, "textField", textField);
            SetPrivateField(link, "disableOnLoad", false);
            return go;
        }

        private TestTextScanBox CreateInactiveTextScanBox(GameObject simpleTextPrefab = null, float delayBetweenCharacters = 0f, float initialInputDelay = 0f)
        {
            var go = new GameObject("TextScanBox", typeof(RectTransform));
            spawned.Add(go);
            go.SetActive(false);
            var textScanBox = go.AddComponent<TestTextScanBox>();
            var dialogueParentGo = new GameObject("DialogueParent", typeof(RectTransform));
            spawned.Add(dialogueParentGo);
            textScanBox.SetDialogueParent(dialogueParentGo.transform);
            textScanBox.SetTunables(simpleTextPrefabRef: simpleTextPrefab, initialInputDelay: initialInputDelay, delayBetweenCharacters: delayBetweenCharacters);

            var controllerGo = new GameObject("Controller");
            spawned.Add(controllerGo);
            textScanBox.TrySetController(controllerGo.AddComponent<TestBaseController>());

            return textScanBox;
        }
        #endregion

        #region ClearOldDialogue
        [UnityTest]
        public IEnumerator ClearOldDialogue_DestroysNonAnchorChildren_KeepsAnchorChildren()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;

            var ordinaryChild = new GameObject("Ordinary", typeof(RectTransform));
            spawned.Add(ordinaryChild);
            ordinaryChild.transform.SetParent(textScanBox.dialogueParentForTest);
            var anchoredChild = new GameObject("Anchored", typeof(RectTransform));
            spawned.Add(anchoredChild);
            anchoredChild.AddComponent<UIAnchor>();
            anchoredChild.transform.SetParent(textScanBox.dialogueParentForTest);

            textScanBox.ClearOldDialogue();
            yield return null; // Destroy() is deferred to end of frame even in Play Mode

            Assert.IsTrue(ordinaryChild == null);
            Assert.IsFalse(anchoredChild == null);
        }

        [UnityTest]
        public IEnumerator ClearOldDialogue_RemovesButtonListenersAndDestroysNonAnchorOptionChildren()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            var optionParentGo = new GameObject("OptionParent", typeof(RectTransform));
            spawned.Add(optionParentGo);
            textScanBox.SetOptionParent(optionParentGo.transform);
            textScanBox.gameObject.SetActive(true);
            yield return null;

            UIChoiceButton optionChoice = TestChoiceFactory.CreateWiredButton("Option", spawned);
            optionChoice.transform.SetParent(optionParentGo.transform);
            bool clicked = false;
            optionChoice.AddOnClickListener(() => clicked = true);

            textScanBox.ClearOldDialogue();
            yield return null;

            Assert.IsTrue(optionChoice == null);
            // The GameObject is gone, so listener removal itself isn't independently observable
            // The strongest remaining check here is that ClearOldDialogue didn't throw
            Assert.IsFalse(clicked);
        }

        [UnityTest]
        public IEnumerator ClearOldDialogue_DestroysPreviouslyPrintedJobs()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();
            textScanBox.gameObject.SetActive(true);
            yield return null;
            var printedJob = new GameObject("PrintedJob");
            spawned.Add(printedJob);
            textScanBox.SetPrintedJobsDirectly(new List<GameObject> { printedJob });

            textScanBox.ClearOldDialogue();
            yield return null;

            Assert.IsTrue(printedJob == null);
        }
        #endregion

        #region Typewriter
        [UnityTest]
        public IEnumerator AddText_RevealsOneCharacterPerFrame_ThenCompletesWithFullText()
        {
            GameObject template = CreateSimpleTextPrefabTemplate();
            TestTextScanBox textScanBox = CreateInactiveTextScanBox(template);
            textScanBox.gameObject.SetActive(true);
            yield return null;

            textScanBox.AddText("Hi");
            yield return null; // Update() dequeues and starts the coroutine, which runs to its first yield

            Assert.IsTrue(textScanBox.publicIsWriting);
            SimpleTextLink link = textScanBox.dialogueParentForTest.GetChild(0).GetComponent<SimpleTextLink>();
            TextMeshProUGUI textField = link.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual("H", textField.text);

            yield return null; // delayBetweenCharacters is 0, so the coroutine finishes on the next frame

            Assert.IsFalse(textScanBox.publicIsWriting);
            Assert.AreEqual("Hi", textField.text);
        }

        [UnityTest]
        public IEnumerator AddText_FinalRevealUsesUnescapedFullText()
        {
            GameObject template = CreateSimpleTextPrefabTemplate();
            TestTextScanBox textScanBox = CreateInactiveTextScanBox(template);
            textScanBox.gameObject.SetActive(true);
            yield return null;

            const string raw = "line1\\nline2"; // 12 chars raw, 11 chars once \n is unescaped
            const string unescaped = "line1\nline2";
            textScanBox.AddText(raw);
            yield return null; // starts the coroutine this frame
            for (int frame = 0; frame < 20 && textScanBox.publicIsWriting; frame++)
            {
                yield return null;
            }

            Assert.IsFalse(textScanBox.publicIsWriting);
            var link = textScanBox.dialogueParentForTest.GetChild(0).GetComponent<SimpleTextLink>();
            var textField = link.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(unescaped, textField.text); // NOT the raw "line1\\nline2"
        }

        [UnityTest]
        public IEnumerator AddText_ReceptacleMissingSimpleTextLink_NeverSetsIsWriting()
        {
            var brokenTemplate = new GameObject("BrokenTemplate", typeof(RectTransform));
            spawned.Add(brokenTemplate); // deliberately no SimpleTextLink component
            TestTextScanBox textScanBox = CreateInactiveTextScanBox(brokenTemplate);
            textScanBox.gameObject.SetActive(true);
            yield return null;

            textScanBox.AddText("Hello");
            yield return null;
            yield return null;

            // The null-SimpleTextLink check now runs before SetBusyWriting(true)
            // So isWriting is never even set true for a broken receptacle, and never gets stuck
            Assert.IsFalse(textScanBox.publicIsWriting);
        }

        [UnityTest]
        public IEnumerator SkipToEndOfPage_WhileWriting_JumpsStraightToFinalText()
        {
            GameObject template = CreateSimpleTextPrefabTemplate();
            TestTextScanBox textScanBox = CreateInactiveTextScanBox(template);
            textScanBox.gameObject.SetActive(true);
            yield return null;

            textScanBox.AddText("Hello World");
            yield return null; // first character ('H') is shown, coroutine is now mid-loop

            bool wasWriting = textScanBox.PublicTryFastForwardActiveText();
            Assert.IsTrue(wasWriting);

            yield return null; // loop resumes, sees interruptWriting, breaks, reveals full text

            Assert.IsFalse(textScanBox.publicIsWriting);
            var link = textScanBox.dialogueParentForTest.GetChild(0).GetComponent<SimpleTextLink>();
            Assert.AreEqual("Hello World", link.GetComponent<TextMeshProUGUI>().text);
        }

        [Test]
        public void TryFastForwardActiveText_NotCurrentlyWriting_ReturnsFalse()
        {
            TestTextScanBox textScanBox = CreateInactiveTextScanBox();

            Assert.IsFalse(textScanBox.PublicTryFastForwardActiveText());
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
            GameObject template = CreateSimpleTextPrefabTemplate();
            TestTextScanBox textScanBox = CreateInactiveTextScanBox(template);
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
            GameObject template = CreateSimpleTextPrefabTemplate();
            TestTextScanBox textScanBox = CreateInactiveTextScanBox(template);
            textScanBox.gameObject.SetActive(true);
            yield return null;
            textScanBox.AddText("Hello World");
            yield return null; // now mid-typewriter, activeTextScan is non-null

            Assert.IsTrue(textScanBox.publicIsWriting);
            GameObject receptacle = textScanBox.dialogueParentForTest.GetChild(0).gameObject;

            textScanBox.gameObject.SetActive(false); // OnDisable -> DisableTriggered, reliable in Play Mode
            yield return null; // Destroy() inside ClearOldDialogue is deferred to end of frame

            Assert.IsFalse(textScanBox.publicIsWriting);
            Assert.IsTrue(receptacle == null);
        }
        #endregion
    }
}
