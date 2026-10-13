using System;
using UnityEngine;
using UnityEngine.Localization;
using LowDefMustard.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [Serializable]
    public sealed class FrameFlavourOption
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedName;
        [SerializeField] private Color colour = Color.white;

        public LocalizedString GetLocalizedName() => localizedName;
        public string GetName() => localizedName.GetSafeLocalizedString();
        public Color GetColour() => colour;
    }
}
