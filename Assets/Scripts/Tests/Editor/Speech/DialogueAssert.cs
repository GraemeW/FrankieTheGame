using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Frankie.Speech;

namespace Frankie.Tests.Editor
{
    internal static class DialogueAssert
    {
        public static void NodeNames(IEnumerable<DialogueNode> resolved, params string[] expectedNames)
        {
            CollectionAssert.AreEqual(expectedNames, resolved.Select(node => node.name).ToArray());
        }
    }
}
