using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class HlslIncludeTests
    {
        // Note:  Includes are project-relative paths, so moving / renaming a library file breaks every shader that includes it (compile errors on the next import)

        // Const Tunables
        private const string _hlslRoot = "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities";
        private static readonly Regex _projectInclude = new("#include\\s+\"((?:Assets|Packages)/[^\"]+)\"");

        // Tests
        [Test]
        public void ProjectRelativeIncludes_ResolveToExistingFiles()
        {
            string[] hlslFiles = Directory.GetFiles(_hlslRoot, "*.hlsl", SearchOption.AllDirectories);
            Assert.IsNotEmpty(hlslFiles);

            foreach (string hlslFile in hlslFiles)
            {
                foreach (Match include in _projectInclude.Matches(File.ReadAllText(hlslFile)))
                {
                    string includePath = include.Groups[1].Value;
                    Assert.IsTrue(File.Exists(includePath), $"{Path.GetFileName(hlslFile)} includes missing {includePath}");
                }
            }
        }

        [Test]
        public void Includes_NeverReachIntoAssets()
        {
            foreach (string hlslFile in Directory.GetFiles(_hlslRoot, "*.hlsl", SearchOption.AllDirectories))
            {
                StringAssert.DoesNotContain("#include \"Assets/", File.ReadAllText(hlslFile), Path.GetFileName(hlslFile));
            }
        }
    }
}
