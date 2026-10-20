using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectTintTests
    {
        // Note:  ApplyTo is untested ~ it only writes into a MaterialDefinition, which can't be read back

        [Test]
        public void DefaultConstructor_IsSolidWhite()
        {
            Assert.AreEqual(Color.white, new BattleEffectTint().GetColourA());
            StringAssert.Contains($"\"{BattleEffectTint.modeField}\":{(int)BattleEffectTintMode.Solid}", JsonUtility.ToJson(new BattleEffectTint()));
        }

        [Test]
        public void ColourConstructor_SetsColourA()
        {
            Assert.AreEqual(Color.red, new BattleEffectTint(Color.red).GetColourA());
        }

        [Test]
        public void TintSettings_PackIntoTheShadersVectorOrder()
        {
            var settings = new BattleEffectTintSettings(BattleEffectTintMode.Radial, 1.5f, 2f, 0.25f);

            Assert.AreEqual(new Vector4((int)BattleEffectTintMode.Radial, 1.5f, 2f, 0.25f), settings.ToVector4());
        }

        [Test]
        public void FieldReferences_MatchTheSerializedFieldNames()
        {
            // The tint drawer shows / hides fields by these names
            string json = JsonUtility.ToJson(new BattleEffectTint());

            foreach (string fieldName in new[] { BattleEffectTint.modeField, BattleEffectTint.colourBField, BattleEffectTint.angleField, BattleEffectTint.frequencyField, BattleEffectTint.speedField })
            {
                StringAssert.Contains($"\"{fieldName}\"", json);
            }
        }
    }
}
