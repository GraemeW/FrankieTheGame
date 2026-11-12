using System;
using System.Linq;

namespace LowDefMustard.Utils.Tests
{
    public static class TestAssemblyCheck
    {
        private const string _runtimeTestsSuffix = ".Tests";
        private const string _editorTestsSuffix = ".Tests.Editor";
        private const string _nunitAssemblyName = "nunit.framework";

        public static bool IsTestAssemblyType(Type type)
        {
            if (type == null) { return false; }
            string assemblyName = type.Assembly.GetName().Name;
            return assemblyName.EndsWith(_runtimeTestsSuffix, StringComparison.Ordinal)
                   || assemblyName.EndsWith(_editorTestsSuffix, StringComparison.Ordinal)
                   || type.Assembly.GetReferencedAssemblies().Any(referencedAssembly => referencedAssembly.Name == _nunitAssemblyName);
        }
    }
}
