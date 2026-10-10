using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [CreateAssetMenu(fileName = "FrameFlavours", menuName = "UI/Frame Flavours", order = 42)]
    public sealed class FrameFlavours : ScriptableObject, ILocalizable
    {
        // Note:  Deliberately not linked to ILocalizable auto-update/teardown on rename/delete

        // Tunables
        [SerializeField] private List<FrameFlavourOption> flavours = new();

        public IReadOnlyList<FrameFlavourOption> GetFlavours() => flavours;

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var localizationEntries = new List<TableEntryReference>();
            foreach (FrameFlavourOption flavour in flavours)
            {
                if (flavour?.GetLocalizedName() == null) { continue; }
                localizationEntries.Add(flavour.GetLocalizedName().TableEntryReference);
            }
            return localizationEntries;
        }
        #endregion
    }
}
