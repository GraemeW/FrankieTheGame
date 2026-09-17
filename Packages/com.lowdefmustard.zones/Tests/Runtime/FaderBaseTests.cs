using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests
{
    public class FaderBaseTests
    {
        // State
        private GameObject faderGameObject;
        private GameObject imageGameObject;
        private TestFader fader;
        private FaderBase<TestTransitionType> originalActiveFader;

        #region DataStructures
        private enum TestTransitionType { None, Fade }

        private class TestFader : FaderBase<TestTransitionType>
        {
            protected override TestTransitionType GetSceneLoadTransitionType() => TestTransitionType.Fade;
            protected override void TriggerSave() { }
            protected override void TriggerLoad() { }

            public void SetNodeEntryForTest(Image image) => nodeEntry = image;
            public bool IsFadingForTest() => fading;
        }
        #endregion
        
        #region Setup
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            originalActiveFader = FaderBase<TestTransitionType>.activeFader;

            imageGameObject = new GameObject("NodeEntry", typeof(Image));
            imageGameObject.SetActive(false);

            faderGameObject = new GameObject("TestFader");
            fader = faderGameObject.AddComponent<TestFader>();
            fader.SetNodeEntryForTest(imageGameObject.GetComponent<Image>());

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
            if (imageGameObject != null) { Object.Destroy(imageGameObject); }
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
