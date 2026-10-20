using NUnit.Framework;
using UnityEngine;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectRecipeTests
    {
        // Notes:
        //  - Bakes via the internal BakeDataTexture ~ since ApplyTo writes into a MaterialDefinition, which can't be read back
        //  - ValidateShapes is called directly (it normally runs from OnValidate on inspector edits)

        // State
        private BattleEffectRecipe recipe;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            recipe = ScriptableObject.CreateInstance<BattleEffectRecipe>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(recipe);
        }
        #endregion

        #region Bake
        [Test]
        public void BakeDataTexture_EmptyRecipe_HasOneRowAndNoItems()
        {
            Texture2D dataTexture = recipe.BakeDataTexture(out int itemCount);

            Assert.AreEqual(0, itemCount);
            Assert.AreEqual(BattleEffectDataLayout.texelsPerItem, dataTexture.width);
            Assert.AreEqual(1, dataTexture.height);
            Assert.AreEqual(TextureFormat.RGBAFloat, dataTexture.format);
            Assert.AreEqual(FilterMode.Point, dataTexture.filterMode);
        }

        [Test]
        public void BakeDataTexture_ShapelessItem_IsSkippedAndRowsPacked()
        {
            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle()), CreateItem(null), CreateItem(new BattleEffectDiamond()) });

            Texture2D dataTexture = recipe.BakeDataTexture(out int itemCount);

            Assert.AreEqual(2, itemCount);
            Assert.AreEqual(3, dataTexture.height);
            Assert.AreEqual(0f, dataTexture.GetPixel(BattleEffectDataLayout.header, 0).r, "row 0 shape ID (circle)");
            Assert.AreEqual(2f, dataTexture.GetPixel(BattleEffectDataLayout.header, 1).r, "row 1 shape ID (diamond)");
            AssertTexel(Color.clear, dataTexture.GetPixel(BattleEffectDataLayout.header, 2), "unused last row");
        }

        [Test]
        public void BakeDataTexture_AfterEdit_UpdatesTheSameTextureInPlace()
        {
            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle()) });
            Texture2D firstBake = recipe.BakeDataTexture(out _);

            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle()), CreateItem(new BattleEffectChevron()) });
            Texture2D secondBake = recipe.BakeDataTexture(out int itemCount);

            Assert.AreSame(firstBake, secondBake);
            Assert.AreEqual(2, itemCount);
            Assert.AreEqual(2, secondBake.height);
            Assert.AreEqual(4f, secondBake.GetPixel(BattleEffectDataLayout.header, 1).r, "row 1 shape ID (chevron)");
        }

        [Test]
        public void BakeDataTexture_RecipeScale_IsPassedToItems()
        {
            JsonUtility.FromJsonOverwrite("{\"scaleRatio\":2}", recipe);
            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle()) });

            Texture2D dataTexture = recipe.BakeDataTexture(out _);

            AssertTexel(new Color(0.5f, 0.5f, 0.5f, 0.5f), dataTexture.GetPixel(BattleEffectDataLayout.scale, 0), "default 0.25 item scale x 2");
        }
        #endregion

        #region Accessors
        [Test]
        public void GetEndTime_ReturnsTheLatestItemEnd()
        {
            recipe.SetItems(new[]
            {
                CreateItem(new BattleEffectCircle(), "{\"startTimeSeconds\":0.5,\"durationSeconds\":1}"),
                CreateItem(new BattleEffectCircle(), "{\"startTimeSeconds\":0.2,\"durationSeconds\":2}"),
                null
            });

            Assert.AreEqual(2.2f, recipe.GetEndTime(), 1e-5f);
        }

        [Test]
        public void GetEndTime_EmptyRecipe_IsZero()
        {
            Assert.AreEqual(0f, recipe.GetEndTime());
        }

        [Test]
        public void SetItems_IncrementsRevision()
        {
            int revision = recipe.GetRevision();

            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle()) });

            Assert.Greater(recipe.GetRevision(), revision);
        }
        #endregion

        #region ValidateShapes
        [Test]
        public void ValidateShapes_NullItem_ReplacedWithADefaultCircleItem()
        {
            recipe.SetItems(new BattleEffectItem[] { null });

            recipe.ValidateShapes();

            Assert.IsNotNull(recipe.GetItems()[0]);
            Assert.IsInstanceOf<BattleEffectCircle>(recipe.GetItems()[0].GetShape());
        }

        [Test]
        public void ValidateShapes_ZeroFilledItem_ResetToDefaults()
        {
            recipe.SetItems(new[] { CreateItem(null, $"{{\"durationSeconds\":0,\"colour\":{ToJson(Color.clear)}}}") });

            recipe.ValidateShapes();

            BattleEffectItem item = recipe.GetItems()[0];
            Assert.IsFalse(item.IsUnset());
            Assert.AreEqual(1f, item.GetEndTime(), 1e-5f, "default duration");
            Assert.IsInstanceOf<BattleEffectCircle>(item.GetShape());
        }

        [Test]
        public void ValidateShapes_ShapelessItem_DefaultsToCircleAndKeepsItsSettings()
        {
            recipe.SetItems(new[] { CreateItem(null, "{\"startTimeSeconds\":0.75,\"durationSeconds\":0.5}") });

            recipe.ValidateShapes();

            BattleEffectItem item = recipe.GetItems()[0];
            Assert.IsInstanceOf<BattleEffectCircle>(item.GetShape());
            Assert.AreEqual(0.75f, item.GetStartTime(), 1e-5f);
        }

        [Test]
        public void ValidateShapes_SharedShapeInstance_SplitIntoEqualCopies()
        {
            // i.e. a duplicated inspector list entry shares its [SerializeReference] shape w/ the original
            var sharedShape = new BattleEffectDiamond();
            JsonUtility.FromJsonOverwrite("{\"aspectRatio\":{\"start\":0.5,\"end\":2}}", sharedShape);
            recipe.SetItems(new[] { CreateItem(sharedShape), CreateItem(sharedShape) });

            recipe.ValidateShapes();

            BattleEffectShape first = recipe.GetItems()[0].GetShape();
            BattleEffectShape second = recipe.GetItems()[1].GetShape();
            Assert.AreNotSame(first, second);
            Assert.IsInstanceOf<BattleEffectDiamond>(second);
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            second.GetParameters(parameters);
            Assert.AreEqual(0.5f, parameters[0].GetStart(), 1e-5f);
            Assert.AreEqual(2f, parameters[0].GetEnd(), 1e-5f);
        }
        #endregion
    }
}
