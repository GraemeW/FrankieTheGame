using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests
{
    public class FaderBaseTests
    {
        // State
        private GameObject faderGameObject;
        private TestFader fader;
        private FaderBase<TestTransitionType> originalActiveFader;

        #region DataStructures
        private enum TestTransitionType { None, Fade }

        private class TestFader : FaderBase<TestTransitionType>
        {
            public readonly List<float> overlayAlphas = new();
            
            protected override TestTransitionType GetSceneLoadTransitionType() => TestTransitionType.Fade;
            protected override void TriggerSave() { }
            protected override void TriggerLoad() { }

            protected override void SetOverlayAlpha(TestTransitionType transitionType, float alpha) => overlayAlphas.Add(alpha);
            protected override void ResetOverlays() => overlayAlphas.Clear();
            public bool IsFadingForTest() => fading;
        }
        #endregion
        
        #region Setup
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            originalActiveFader = FaderBase<TestTransitionType>.activeFader;

            faderGameObject = new GameObject("TestFader");
            fader = faderGameObject.AddComponent<TestFader>();

            fader.fadeInTimer = 0.05f;
            fader.fadeOutTimer = 0.05f;
            fader.zoneFadeTimerMultiplier = 1f;

            FaderBase<TestTransitionType>.activeFader = fader;

            // Let Awake/OnEnable/Start settle before any test drives a fade
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            FaderBase<TestTransitionType>.activeFader = originalActiveFader;
            if (faderGameObject != null) { Object.Destroy(faderGameObject); }
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator StartStandardFade_RunsFullSequence_FiresAllCallbacksInOrder()
        {
            var callOrder = new List<string>();
            var triggers = new FaderEventTriggers<TestTransitionType>(
                _ => callOrder.Add("fadeIn"),
                () => callOrder.Add("fadePeak"),
                () => callOrder.Add("fadeOut"),
                () => callOrder.Add("fadeComplete"));

            bool started = FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers);
            Assert.IsTrue(started);

            yield return new WaitForSeconds(0.3f);

            CollectionAssert.AreEqual(new[] { "fadeIn", "fadePeak", "fadeOut", "fadeComplete" }, callOrder);
            Assert.IsFalse(fader.IsFadingForTest());
        }

        [UnityTest]
        public IEnumerator StartStandardFade_DrivesOverlayAlpha_UpToOpaqueThenBackToHidden()
        {
            var triggers = new FaderEventTriggers<TestTransitionType>(null, null, null, null);

            bool started = FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers);
            Assert.IsTrue(started);

            yield return new WaitForSeconds(0.3f);

            List<float> overlayAlphas = fader.overlayAlphas;
            int peakIndex = overlayAlphas.IndexOf(1f);
            Assert.AreEqual(0f, overlayAlphas[0]);
            Assert.GreaterOrEqual(peakIndex, 0);
            Assert.AreEqual(0f, overlayAlphas[^1]);
            for (int i = 1; i <= peakIndex; i++) { Assert.GreaterOrEqual(overlayAlphas[i], overlayAlphas[i - 1]); }
            for (int i = peakIndex + 2; i < overlayAlphas.Count; i++) { Assert.LessOrEqual(overlayAlphas[i], overlayAlphas[i - 1]); }
        }

        [UnityTest]
        public IEnumerator Disable_MidFade_ResetsOverlaysAndAllowsNextFade()
        {
            var triggers = new FaderEventTriggers<TestTransitionType>(null, null, null, null);
            Assert.IsTrue(FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers));
            yield return null;
            Assert.IsNotEmpty(fader.overlayAlphas);

            fader.enabled = false;
            Assert.IsFalse(fader.IsFadingForTest());
            Assert.IsEmpty(fader.overlayAlphas);

            fader.enabled = true;
            Assert.IsTrue(FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers));
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(fader.IsFadingForTest());
        }

        [UnityTest]
        public IEnumerator StartStandardFade_WhileAlreadyFading_ReturnsFalse()
        {
            var triggers = new FaderEventTriggers<TestTransitionType>(null, null, null, null);
            
            bool started = FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers);
            Assert.IsTrue(started);
            
            bool secondCallResult = FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers);
            Assert.IsFalse(secondCallResult);

            // Let the first fade finish so it doesn't bleed into the next test
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator StartBlipFade_HoldsForGivenDuration_ThenFiresFadeOutAndComplete()
        {
            var callOrder = new List<string>();
            var triggers = new FaderEventTriggers<TestTransitionType>(
                _ => callOrder.Add("fadeIn"),
                () => callOrder.Add("fadePeak"),
                () => callOrder.Add("fadeOut"),
                () => callOrder.Add("fadeComplete"));

            bool started = FaderBase<TestTransitionType>.StartBlipFade(0.05f, triggers);
            Assert.IsTrue(started);

            yield return new WaitForSeconds(0.4f);

            CollectionAssert.AreEqual(new[] { "fadeIn", "fadePeak", "fadeOut", "fadeComplete" }, callOrder);
            Assert.IsFalse(fader.IsFadingForTest());
        }

        [UnityTest]
        public IEnumerator TryQueueAtFadePeak_NoFade_ReturnsFalseAndDoesNotRun()
        {
            var runCount = 0;

            Assert.IsFalse(FaderBase<TestTransitionType>.TryQueueAtFadePeak(() => runCount++));
            yield return null;

            Assert.AreEqual(0, runCount);
        }

        [UnityTest]
        public IEnumerator TryQueueAtFadePeak_DuringFadeIn_RunsOnceAtPeakBeforeOnFadePeak()
        {
            var callOrder = new List<string>();
            var triggers = new FaderEventTriggers<TestTransitionType>(null, () => callOrder.Add("fadePeak"), () => callOrder.Add("fadeOut"), null);
            Assert.IsTrue(FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers));

            Assert.IsTrue(FaderBase<TestTransitionType>.TryQueueAtFadePeak(() => callOrder.Add("queued")));
            CollectionAssert.IsEmpty(callOrder, "Queued action ran before the peak");
            yield return new WaitForSeconds(0.3f);

            CollectionAssert.AreEqual(new[] { "queued", "fadePeak", "fadeOut" }, callOrder);

            Assert.IsTrue(FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers));
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, callOrder.FindAll(call => call == "queued").Count, "Queued action is one-shot");
        }

        [UnityTest]
        public IEnumerator TryQueueAtFadePeak_AfterPeak_ReturnsFalse()
        {
            var peaked = false;
            var triggers = new FaderEventTriggers<TestTransitionType>(null, () => peaked = true, null, null);
            Assert.IsTrue(FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers));
            while (!peaked) { yield return null; }

            Assert.IsTrue(fader.IsFadingForTest(), "Fade should still be on its way out");
            Assert.IsFalse(FaderBase<TestTransitionType>.TryQueueAtFadePeak(() => { }));

            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator Disable_MidFadeIn_StillRunsQueuedAction()
        {
            var runCount = 0;
            var triggers = new FaderEventTriggers<TestTransitionType>(null, null, null, null);
            Assert.IsTrue(FaderBase<TestTransitionType>.StartStandardFade(TestTransitionType.Fade, triggers));
            Assert.IsTrue(FaderBase<TestTransitionType>.TryQueueAtFadePeak(() => runCount++));
            yield return null;

            fader.enabled = false;

            Assert.AreEqual(1, runCount);
            Assert.IsFalse(FaderBase<TestTransitionType>.TryQueueAtFadePeak(() => runCount++));
        }
        #endregion
    }
}
