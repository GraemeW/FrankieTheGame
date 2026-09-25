using System.Collections;
using System.IO;
using LowDefMustard.Zones.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.Zones.Tests.Editor
{
    // Covers TryLoadSnapshots (reached via CreateGUI, matching CreateGUI_ActiveMultiZoneViewNull's precedent above)
    public class MultiZoneViewerSnapshotTests
    {
        // Const Tunables
        private const string _scratchFolderName = "ScratchTest_ConduitLinkFollowing_SafeToDelete";
        private const string _scratchFolder = "Assets/" + _scratchFolderName;
        private const string _scratchMultiZoneViewName = "ScratchTest_MultiZoneViewerSnapshotTests_SafeToDelete";
        private const float _tolerance = 0.01f;

        // State
        private MultiZoneViewer viewer;
        private MultiZoneView multiZoneView;
        private string validSnapshotPath;
        private bool attached;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(_scratchFolder)) { AssetDatabase.DeleteAsset(_scratchFolder); }
            AssetDatabase.CreateFolder("Assets", _scratchFolderName);
            
            viewer = ScriptableObject.CreateInstance<MultiZoneViewer>();
            
            var multiZoneViewInstance = ScriptableObject.CreateInstance<MultiZoneView>();
            string path = $"{_scratchFolder}/{_scratchMultiZoneViewName}.asset";
            AssetDatabase.CreateAsset(multiZoneViewInstance, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            multiZoneView = AssetDatabase.LoadAssetAtPath<MultiZoneView>(path);
            
            attached = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ZoneView zoneView in viewer.zoneViews)
            {
                if (zoneView.texture2D != null) { Object.DestroyImmediate(zoneView.texture2D); }
            }
            if (viewer != null)
            {
                if (attached) { viewer.Close(); }
                else { Object.DestroyImmediate(viewer); }
            }
            Object.DestroyImmediate(multiZoneView, true);
            if (validSnapshotPath != null && File.Exists(validSnapshotPath)) { File.Delete(validSnapshotPath); }
            
            AssetDatabase.DeleteAsset(_scratchFolder);
            AssetDatabase.Refresh();
        }
        #endregion

        #region PrivateMethods
        private string WritePNG(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            byte[] pngBytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            string path = Path.Combine(Application.temporaryCachePath, $"ScratchSnapshotTest_{width}x{height}.png");
            File.WriteAllBytes(path, pngBytes);
            return path;
        }

        private void Attach()
        {
            viewer.ShowUtility();
            viewer.position = new Rect(-10000, -10000, 400, 300);
            attached = true;
        }
        #endregion

        #region Tests
        [Test]
        public void CreateGUI_ActiveMultiZoneViewWithValidSnapshot_LoadsTextureIntoZoneViewsAndLookup()
        {
            validSnapshotPath = WritePNG(64, 48);
            multiZoneView.CreateOrUpdateZoneViewData("ZoneA", "Assets/ZoneA.unity", validSnapshotPath, new Vector2(200f, 150f), Vector2.zero, false, false);
            viewer.activeMultiZoneView = multiZoneView;

            viewer.CreateGUI();

            Assert.AreEqual(1, viewer.zoneViews.Count);
            Assert.IsNotNull(viewer.zoneViews[0].texture2D);
            Assert.AreEqual(64, viewer.zoneViews[0].texture2D.width);
            Assert.AreEqual(48, viewer.zoneViews[0].texture2D.height);
            Assert.IsTrue(viewer.zoneViewLookup.ContainsKey("ZoneA"));
            Assert.AreSame(viewer.zoneViews[0], viewer.zoneViewLookup["ZoneA"]);
        }

        [Test]
        public void CreateGUI_SnapshotPathMissingOnDisk_SkipsThatZoneViewEntirely()
        {
            multiZoneView.CreateOrUpdateZoneViewData("ZoneA", "Assets/ZoneA.unity", Path.Combine(Application.temporaryCachePath, "ScratchSnapshotTest_DoesNotExist.png"), new Vector2(200f, 150f), Vector2.zero, false, false);
            viewer.activeMultiZoneView = multiZoneView;

            viewer.CreateGUI();

            Assert.AreEqual(0, viewer.zoneViews.Count);
            Assert.IsFalse(viewer.zoneViewLookup.ContainsKey("ZoneA"));
        }

        [Test]
        public void CreateGUI_SnapshotPathEmpty_SkipsThatZoneViewEntirely()
        {
            multiZoneView.CreateOrUpdateZoneViewData("ZoneA", "Assets/ZoneA.unity", string.Empty, new Vector2(200f, 150f), Vector2.zero, false, false);
            viewer.activeMultiZoneView = multiZoneView;

            viewer.CreateGUI();

            Assert.AreEqual(0, viewer.zoneViews.Count);
        }

        [Test]
        public void CreateGUI_MultipleZoneViewData_LoadsOnlyTheOnesWithValidSnapshots()
        {
            validSnapshotPath = WritePNG(32, 32);
            multiZoneView.CreateOrUpdateZoneViewData("ZoneWithSnapshot", "Assets/ZoneWithSnapshot.unity", validSnapshotPath, new Vector2(100f, 100f), Vector2.zero, false, false);
            multiZoneView.CreateOrUpdateZoneViewData("ZoneMissingSnapshot", "Assets/ZoneMissingSnapshot.unity", Path.Combine(Application.temporaryCachePath, "ScratchSnapshotTest_Nope.png"), new Vector2(100f, 100f), Vector2.zero, false, false);
            viewer.activeMultiZoneView = multiZoneView;

            viewer.CreateGUI();

            Assert.AreEqual(1, viewer.zoneViews.Count);
            Assert.AreEqual("ZoneWithSnapshot", viewer.zoneViews[0].data.zoneName);
        }

        [Test]
        public void CreateGUI_ImageSmallerThanZoneView_FitsInsideAndCentresWithOffset()
        {
            // 64x48 image at its "true" size (snapshotToZoneViewScalingFactor default 0.15 -> 9.6x7.2), well inside a 200x150 zone view
            validSnapshotPath = WritePNG(64, 48);
            multiZoneView.CreateOrUpdateZoneViewData("ZoneA", "Assets/ZoneA.unity", validSnapshotPath, new Vector2(200f, 150f), Vector2.zero, false, false);
            viewer.activeMultiZoneView = multiZoneView;

            viewer.CreateGUI();

            ZoneView zoneView = viewer.zoneViews[0];
            Assert.LessOrEqual(zoneView.renderedImageDimensions.x, 200f);
            Assert.LessOrEqual(zoneView.renderedImageDimensions.y, 150f);
            Assert.AreEqual((200f - zoneView.renderedImageDimensions.x) / 2f, zoneView.renderedImageOffset.x, _tolerance);
            Assert.AreEqual((150f - zoneView.renderedImageDimensions.y) / 2f, zoneView.renderedImageOffset.y, _tolerance);
        }

        [Test]
        public void CreateGUI_ImageAspectRatioWiderThanZoneView_ScalesToZoneViewWidthWithVerticalOffset()
        {
            // A very wide, short image forced into a square zone view: width fills exactly, height gets the letterbox offset
            validSnapshotPath = WritePNG(400, 40);
            multiZoneView.CreateOrUpdateZoneViewData("ZoneA", "Assets/ZoneA.unity", validSnapshotPath, new Vector2(200f, 200f), Vector2.zero, false, false);
            viewer.activeMultiZoneView = multiZoneView;

            viewer.CreateGUI();

            ZoneView zoneView = viewer.zoneViews[0];
            Assert.AreEqual(200f, zoneView.renderedImageDimensions.x, _tolerance);
            Assert.Less(zoneView.renderedImageDimensions.y, 200f);
            Assert.AreEqual(0f, zoneView.renderedImageOffset.x, _tolerance);
            Assert.Greater(zoneView.renderedImageOffset.y, 0f);
        }

        [UnityTest]
        public IEnumerator OnClearClicked_RemovesLoadedZoneViewsAndTextures()
        {
            validSnapshotPath = WritePNG(16, 16);
            multiZoneView.CreateOrUpdateZoneViewData("ZoneA", "Assets/ZoneA.unity", validSnapshotPath, new Vector2(100f, 100f), Vector2.zero, false, false);
            viewer.activeMultiZoneView = multiZoneView;
            viewer.CreateGUI();
            Attach();
            yield return null;
            Assert.AreEqual(1, viewer.zoneViews.Count);
            
            HeadlessEditorWindow.SendClick(viewer.clearButton);
            yield return null;

            Assert.AreEqual(0, viewer.zoneViews.Count);
            Assert.IsNull(viewer.activeMultiZoneView);
        }
        #endregion
    }
}
