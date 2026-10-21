using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Rendering.Editor;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectOffscreenRendererTests
    {
        // Note:  Rendering through the renderer is covered by the frame exporter tests (which read its texture back)

        #region Tests
        [Test]
        public void Constructor_CreatesATextureOfTheSize()
        {
            using var offscreenRenderer = new BattleEffectOffscreenRenderer(new Vector2Int(48, 24), Color.black);

            Assert.AreEqual(48, offscreenRenderer.GetTexture().width);
            Assert.AreEqual(24, offscreenRenderer.GetTexture().height);
        }

        [Test]
        public void Constructor_ZeroSize_ClampsToOnePixel()
        {
            using var offscreenRenderer = new BattleEffectOffscreenRenderer(Vector2Int.zero, Color.black);

            Assert.AreEqual(Vector2Int.one, offscreenRenderer.GetSize());
        }

        [Test]
        public void Dispose_ReleasesTheTexture_AndCanRepeat()
        {
            var offscreenRenderer = new BattleEffectOffscreenRenderer(new Vector2Int(8, 8), Color.black);

            offscreenRenderer.Dispose();

            Assert.IsNull(offscreenRenderer.GetTexture());
            Assert.DoesNotThrow(offscreenRenderer.Dispose);
        }

        [Test]
        public void SetMaterial_BeforeThePanelBuilds_IsHeldWithoutThrowing()
        {
            using var offscreenRenderer = new BattleEffectOffscreenRenderer(new Vector2Int(8, 8), Color.black);

            Assert.IsFalse(offscreenRenderer.IsBuilt(), "the panel builds on a later editor update");
            Assert.DoesNotThrow(() => offscreenRenderer.SetMaterial(new StyleMaterialDefinition(StyleKeyword.Null)));
        }
        #endregion
    }
}
