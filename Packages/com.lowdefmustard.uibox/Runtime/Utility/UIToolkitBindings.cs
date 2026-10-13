using Unity.Properties;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public static class UIToolkitBindings
    {
        // Pass source property names via nameof(Model.property) - avoid magic strings in binding paths
        public static DataBinding ToTarget(string sourcePropertyName) => new()
        {
            dataSourcePath = new PropertyPath(sourcePropertyName),
            bindingMode = BindingMode.ToTarget
        };
    }
}
