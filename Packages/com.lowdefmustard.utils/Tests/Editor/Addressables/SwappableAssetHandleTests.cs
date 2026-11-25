using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class SwappableAssetHandleTests
    {
        // Tested:
        //  - TryLoad - loads an asset by reference;  a null / unset reference loads nothing + keeps the current asset
        //  - ReleaseReplaced - a replaced asset stays loaded until released (incl. the same asset loaded twice)
        //  - RetireCurrent - the current asset is kept until ReleaseReplaced, then released
        //  - ReleaseAll - releases the current + replaced assets
        // Not Tested:
        //  - A caller's swap timing (e.g. BackgroundMusic's fades) - needs live playback (manual play)
        // Note:  loads the project's own AudioClip entries (any two) ~ skips if the project has fewer

        // State
        private readonly SwappableAssetHandle<AudioClip> swappableAssetHandle = new();
        private List<AssetReferenceAudioClip> audioClipReferences;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            audioClipReferences = AddressableAssetSettingsDefaultObject.Settings == null ? new List<AssetReferenceAudioClip>() : AddressableAssetSettingsDefaultObject.Settings.groups
                .Where(group => group != null)
                .SelectMany(group => group.entries)
                .Where(entry => AssetDatabase.GetMainAssetTypeAtPath(entry.AssetPath) == typeof(AudioClip))
                .Take(2)
                .Select(entry => new AssetReferenceAudioClip(entry.guid))
                .ToList();
            if (audioClipReferences.Count < 2) { Assert.Ignore("Needs two Addressables AudioClip entries in the project"); }
        }

        [TearDown]
        public void TearDown() => swappableAssetHandle.ReleaseAll();
        #endregion

        #region Helpers
        private AsyncOperationHandle<AudioClip> LoadAndWait(AssetReferenceAudioClip assetReference)
        {
            Assert.IsTrue(swappableAssetHandle.TryLoad(assetReference));
            swappableAssetHandle.current.WaitForCompletion();
            return swappableAssetHandle.current;
        }
        #endregion

        #region Tests
        [Test]
        public void TryLoad_Reference_LoadsTheAsset()
        {
            AsyncOperationHandle<AudioClip> handle = LoadAndWait(audioClipReferences[0]);

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status);
            Assert.IsNotNull(handle.Result);
        }

        [Test]
        public void TryLoad_NullOrUnsetReference_LoadsNothingAndKeepsCurrent()
        {
            AsyncOperationHandle<AudioClip> handle = LoadAndWait(audioClipReferences[0]);

            Assert.IsFalse(swappableAssetHandle.TryLoad(null));
            Assert.IsFalse(swappableAssetHandle.TryLoad(new AssetReferenceAudioClip("")));
            Assert.AreEqual(handle, swappableAssetHandle.current);
        }

        [Test]
        public void ReleaseReplaced_AfterANewAsset_ReleasesOnlyTheOldOne()
        {
            AsyncOperationHandle<AudioClip> oldHandle = LoadAndWait(audioClipReferences[0]);
            AsyncOperationHandle<AudioClip> newHandle = LoadAndWait(audioClipReferences[1]);
            Assert.IsTrue(oldHandle.IsValid(), "Released before the caller was off it");

            swappableAssetHandle.ReleaseReplaced();

            Assert.IsFalse(oldHandle.IsValid());
            Assert.IsTrue(newHandle.IsValid());
        }

        [Test]
        public void ReleaseReplaced_SameAssetLoadedTwice_KeepsTheAssetLoaded()
        {
            LoadAndWait(audioClipReferences[0]);
            AsyncOperationHandle<AudioClip> newHandle = LoadAndWait(audioClipReferences[0]); // Addressables shares the operation + adds a reference

            swappableAssetHandle.ReleaseReplaced();

            Assert.IsTrue(newHandle.IsValid());
            Assert.IsNotNull(newHandle.Result);
        }

        [Test]
        public void RetireCurrent_KeepsTheAssetUntilReleaseReplaced()
        {
            AsyncOperationHandle<AudioClip> retiredHandle = LoadAndWait(audioClipReferences[0]);

            swappableAssetHandle.RetireCurrent();

            Assert.IsFalse(swappableAssetHandle.current.IsValid());
            Assert.IsTrue(retiredHandle.IsValid(), "Released before the caller was off it");
            swappableAssetHandle.ReleaseReplaced();
            Assert.IsFalse(retiredHandle.IsValid());
        }

        [Test]
        public void ReleaseAll_ReleasesCurrentAndReplaced()
        {
            AsyncOperationHandle<AudioClip> oldHandle = LoadAndWait(audioClipReferences[0]);
            AsyncOperationHandle<AudioClip> newHandle = LoadAndWait(audioClipReferences[1]);

            swappableAssetHandle.ReleaseAll();

            Assert.IsFalse(oldHandle.IsValid());
            Assert.IsFalse(newHandle.IsValid());
            Assert.IsFalse(swappableAssetHandle.current.IsValid());
        }
        #endregion
    }
}
