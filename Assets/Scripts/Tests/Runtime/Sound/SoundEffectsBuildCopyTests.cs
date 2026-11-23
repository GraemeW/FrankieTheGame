#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Sound;

namespace Frankie.Tests
{
    public class SoundEffectsBuildCopyTests
    {
        // Tested:
        //  - SoundEffects.PlayClip - a same-named copy of a playing clip isn't started on a second pooled source
        //  - SoundEffects.PlayClip - a different clip still plays on the free source (the pool isn't just full)
        // Not Tested:
        //  - Stacking across separate SoundEffects components / PlayClipAfterDestroy instances - dedupe is per pool by design
        // Test Notes:
        //  - The clip copy is a second AudioClip.Create with the same name, standing in for a build copy
        //  - Inconclusive if the editor can't start audio (AudioSource.isPlaying stays false)

        // Const
        private const string _clipName = "SoundEffectsBuildCopyTests_Clip";
        private const int _clipSamples = 44100 * 5;
        private const int _clipFrequency = 44100;

        // State
        private readonly TestObjectTracker tracker = new();
        private SoundEffects soundEffects;
        private AudioSource firstSource;
        private AudioSource secondSource;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameObject soundObject = tracker.Track(new GameObject("SoundEffectsBuildCopyTests_SoundEffects"));
            soundObject.SetActive(false);
            firstSource = soundObject.AddComponent<AudioSource>();
            secondSource = soundObject.AddComponent<AudioSource>();
            soundEffects = soundObject.AddComponent<SoundEffects>();
            SerializedFieldInjector.SetArray(soundEffects, SoundEffects.audioSourcesField, firstSource, secondSource);
            soundObject.SetActive(true);
            yield return null; // Start stops every source, so play after it
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private AudioClip CreateClip(string clipName) => tracker.Track(AudioClip.Create(clipName, _clipSamples, 1, _clipFrequency, false));

        private void PlayFirstClip(AudioClip audioClip)
        {
            soundEffects.PlayClip(audioClip);
            if (!firstSource.isPlaying) { Assert.Inconclusive("Editor audio unavailable ~ AudioSource never started"); }
        }
        #endregion

        #region Tests
        [Test]
        public void PlayClip_SameNamedCopyPlaying_IsSkipped()
        {
            PlayFirstClip(CreateClip(_clipName));

            soundEffects.PlayClip(CreateClip(_clipName));

            Assert.IsFalse(secondSource.isPlaying, "Copy of the playing clip started on the second source");
        }

        [Test]
        public void PlayClip_DifferentClipPlaying_PlaysOnFreeSource()
        {
            PlayFirstClip(CreateClip(_clipName));
            AudioClip otherClip = CreateClip($"{_clipName}_Other");

            soundEffects.PlayClip(otherClip);

            Assert.IsTrue(secondSource.isPlaying);
            Assert.AreSame(otherClip, secondSource.clip);
        }
        #endregion
    }
}
#endif
