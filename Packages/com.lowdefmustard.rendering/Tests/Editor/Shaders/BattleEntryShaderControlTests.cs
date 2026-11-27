using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEntryShaderControlTests
    {
        // State
        private readonly TestObjectTracker tracker = new();
        private GameObject controlObject;
        private BattleEntryShaderControl battleEntryShaderControl;
        private Material material;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            material = tracker.Track(RenderingTestMaterials.CreatePropertiesMaterial());
            controlObject = tracker.CreateGameObject("_TEMP_BattleEntryShaderControlTests");
            battleEntryShaderControl = controlObject.AddComponent<BattleEntryShaderControl>();
            SerializedFieldInjector.Set(battleEntryShaderControl, BattleEntryShaderControl.battleEntryMaterialField, material);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(controlObject); // Before its material
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void SetBattleEntryParameters_WritesTintPhaseAndFadeTimes()
        {
            battleEntryShaderControl.SetBattleEntryParameters(Color.magenta, 2f, 0.5f);

            Assert.AreEqual(Color.magenta, material.GetColor("_Tint"));
            Assert.AreEqual(Time.time, material.GetFloat("_Phase"), 1e-4f);
            Assert.AreEqual(2f, material.GetFloat("_FadeTime"));
            Assert.AreEqual(0.5f, material.GetFloat("_FadeOutTime"));
        }

        [Test]
        public void SetBattleEntryParameters_NoMaterial_DoesNothing()
        {
            SerializedFieldInjector.Set(battleEntryShaderControl, BattleEntryShaderControl.battleEntryMaterialField, null);

            Assert.DoesNotThrow(() => battleEntryShaderControl.SetBattleEntryParameters(Color.magenta, 2f, 0.5f));
        }
        #endregion
    }
}
