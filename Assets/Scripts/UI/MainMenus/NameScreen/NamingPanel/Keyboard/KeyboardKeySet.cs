using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public abstract class KeyboardKeySet : VisualElement
    {
        // Note:
        //  - Column counts come from the keyboard (bound)
        //  - Key size is fixed via USS --keyboard-key-size, set on the key set itself (.keyboard-keys)
        
        // Const Tunables
        protected const string ussClassName = "keyboard-keys";
        private static readonly CustomStyleProperty<float> _keySizeProperty = new("--keyboard-key-size");

        // State
        private int internalColumns;
        private float keySize = 0f;

        // Note: UxmlAttribute exists for UI Builder previews only - the bound model value overrides it at runtime
        [CreateProperty, UxmlAttribute] public int columns
        {
            get => internalColumns;
            set
            {
                internalColumns = value;
                RefreshWidth();
            }
        }

        protected KeyboardKeySet(string columnsPropertyName, int defaultColumns)
        {
            internalColumns = defaultColumns;
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(HandleCustomStyleResolved);
            SetBinding(nameof(columns), UIToolkitBindings.ToTarget(columnsPropertyName));
        }

        #region PrivateMethods
        private void HandleCustomStyleResolved(CustomStyleResolvedEvent customStyleResolvedEvent)
        {
            keySize = customStyleResolvedEvent.customStyle.TryGetValue(_keySizeProperty, out float resolvedKeySize) ? resolvedKeySize : 0f;
            RefreshWidth();
        }

        private void RefreshWidth()
        {
            if (keySize <= 0f || internalColumns <= 0) { return; }
            style.width = internalColumns * keySize;
        }
        #endregion
    }
}
