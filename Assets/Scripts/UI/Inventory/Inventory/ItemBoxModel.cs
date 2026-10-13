using System;
using System.Collections.Generic;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class ItemBoxModel : BindableModel
    {
        // State
        private string internalCharacterName = "";
        private IReadOnlyList<StatChangeLine> internalStatChanges = Array.Empty<StatChangeLine>();
        private bool internalHasStatChanges = false;

        [CreateProperty] public string characterName
        {
            get => internalCharacterName;
            set => SetProperty(ref internalCharacterName, value);
        }

        [CreateProperty] public IReadOnlyList<StatChangeLine> statChanges // Empty hides the stat comparison
        {
            get => internalStatChanges;
            set
            {
                if (!SetProperty(ref internalStatChanges, value)) { return; }
                hasStatChanges = value is { Count: > 0 };
            }
        }

        [CreateProperty] public bool hasStatChanges
        {
            get => internalHasStatChanges;
            private set => SetProperty(ref internalHasStatChanges, value);
        }
    }
}
