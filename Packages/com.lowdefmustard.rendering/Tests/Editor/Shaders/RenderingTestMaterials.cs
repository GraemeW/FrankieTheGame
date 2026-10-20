using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Rendering.Editor;

namespace LowDefMustard.Rendering.Tests.Editor
{
    internal static class RenderingTestMaterials
    {
        // Notes:
        //  - Property round trips use RenderingTestProperties.shader (declares every property the package sets)
        //  - Rendering tests need a real battle effect material (a UI Toolkit graph) ~ found in the project like the preview window does, else the test is ignored

        // Const Tunables
        private const string _propertiesShaderName = "Hidden/LowDefMustard/Rendering/Tests/RenderingTestProperties";

        // Public Methods
        public static Material CreatePropertiesMaterial()
        {
            Shader shader = Shader.Find(_propertiesShaderName);
            Assert.IsNotNull(shader, $"{_propertiesShaderName} is missing");
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        public static Material FindBattleEffectMaterialOrIgnore()
        {
            var renderSettings = new BattleEffectRenderSettings();
            renderSettings.FindMaterialIfUnset();
            if (renderSettings.material == null) { Assert.Ignore("No BattleEffect material in this project (import the Battle Effect sample)"); }
            return renderSettings.material;
        }
    }
}
