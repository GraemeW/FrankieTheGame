using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Frankie.Speech;

namespace Frankie.Tests.Editor
{
    internal static class DialogueAssert
    {
        public static void NodeIDs(IEnumerable<DialogueNode> resolved, params string[] expectedIDs)
        {
            CollectionAssert.AreEqual(expectedIDs, resolved.Select(node => node.GetNodeID()).ToArray());
        }
    }
}
