using Unity.GraphToolkit.Editor;

namespace Frankie.Speech.Editor
{
    internal static class DialogueGraphNodeExtensions
    {
        public static T GetOptionValue<T>(this INode node, string optionName)
        {
            // Options don't exist yet while a node is being created (Node.OnEnable runs first) ~ fall back to default
            INodeOption option = node.GetNodeOptionByName(optionName);
            return option != null && option.TryGetValue(out T value) ? value : default;
        }

        public static bool HasOptionsDefined(this INode node, string optionName) => node.GetNodeOptionByName(optionName) != null;
    }
}
