using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using LowDefMustard.Rendering.Editor;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectDispatchGeneratorTests
    {
        // Notes:
        //  - Builds the output in memory (no file writes)
        //  - GeneratedFile_OnDisk_IsUpToDate reads the real generated file && depends on the project's shapes

        // State
        private List<(int id, Type type)> shapes;
        private string output;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(BattleEffectDispatchGenerator.TryGetShapes(out shapes), "project shapes are invalid (see console)");
            output = BattleEffectDispatchGenerator.BuildOutput(shapes);
        }
        #endregion

        #region Tests
        [Test]
        public void TryGetShapes_SortsById()
        {
            CollectionAssert.IsOrdered(shapes.Select(shape => shape.id));
        }

        [Test]
        public void BuildOutput_MirrorsEveryDataLayoutConst()
        {
            foreach (FieldInfo field in typeof(BattleEffectDataLayout).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral))
            {
                StringAssert.Contains($"#define BATTLE_EFFECT_LAYOUT_{field.Name.ToUpperInvariant()} {field.GetRawConstantValue()}", output);
            }
        }

        [Test]
        public void BuildOutput_MirrorsMarkedEnumsOnly()
        {
            StringAssert.Contains("#define BATTLE_EFFECT_STYLE_OUTLINE 1", output);
            StringAssert.Contains("#define BATTLE_EFFECT_REPETITION_GRID 3", output);
            StringAssert.Contains("#define BATTLE_EFFECT_REPETITION_RADIAL 4", output);
            StringAssert.Contains("#define BATTLE_EFFECT_FIT_STRETCH 1", output);
            StringAssert.DoesNotContain("BATTLE_EFFECT_EASING_", output, "BattleEffectEasing has no [BattleEffectMirroredEnum]");
        }

        [Test]
        public void BuildOutput_DispatchesEveryShapeToItsAdapter()
        {
            foreach ((int id, Type type) in shapes)
            {
                StringAssert.Contains($"if (shapeId == {id}) {{ return {type.Name}Distance(input); }}", output);
            }
        }

        [Test]
        public void BuildOutput_IncludesTheShapeAdaptersFromThePackage()
        {
            StringAssert.Contains("#include \"Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectShapes.hlsl\"", output);
        }

        [Test]
        public void GeneratedFile_OnDisk_IsUpToDate()
        {
            Assert.IsTrue(File.Exists(BattleEffectDispatchGenerator.outputPath));
            Assert.AreEqual(output, File.ReadAllText(BattleEffectDispatchGenerator.outputPath));
        }
        #endregion
    }
}
