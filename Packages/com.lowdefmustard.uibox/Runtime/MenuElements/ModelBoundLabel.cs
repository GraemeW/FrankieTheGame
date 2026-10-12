using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // Typed labels bound to a property on the inherited data source, e.g. `public MyHeaderLabel() : base(nameof(MyMenuModel.headerText)) { }`
    // Note:  The authored `text` attribute shows in UI Builder previews - the bound model value overrides it at runtime
    public abstract class ModelBoundLabel : Label
    {

        protected ModelBoundLabel(string sourcePropertyName)
        {
            AddToClassList(USSClassNames.ModelBoundLabel.block);
            SetBinding(nameof(text), UIToolkitBindings.ToTarget(sourcePropertyName));
            this.RegisterValueChangedCallback(changeEvent => EnableInClassList(USSClassNames.ModelBoundLabel.empty, string.IsNullOrEmpty(changeEvent.newValue))); // Empty text collapses the label
        }
    }
}
