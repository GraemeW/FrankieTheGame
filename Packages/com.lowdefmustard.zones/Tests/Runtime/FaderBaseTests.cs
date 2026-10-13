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
        #endregion
    }
}
