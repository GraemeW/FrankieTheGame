using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class CreditsListElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "credits-list";

        // State
        private IReadOnlyList<CreditsLine> internalLines;

        [CreateProperty] public IReadOnlyList<CreditsLine> lines
        {
            get => internalLines;
            set
            {
                internalLines = value;
                Rebuild();
            }
        }

        public CreditsListElement()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(lines), UIToolkitBindings.ToTarget(nameof(GameWinMenuModel.creditsLines)));
        }

        private void Rebuild()
        {
            Clear();
            if (internalLines == null) { return; }
            foreach (CreditsLine creditsLine in internalLines)
            {
                Add(new CreditsEntryElement { titleText = creditsLine.title, personName = creditsLine.name });
            }
        }
    }
}
