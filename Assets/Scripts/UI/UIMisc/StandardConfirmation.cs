using System.Collections.Generic;
using Frankie.Utils.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Frankie.Utils.UI
{
    public class StandardConfirmation : MonoBehaviour, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedRejectText;
        [Header("Hookups")] 
        [SerializeField] private UIChoiceButton confirmChoice;
        [SerializeField] private UIChoiceButton rejectChoice;
        
        #region UnityMethods
        private void Start()
        {
            if (confirmChoice != null) { confirmChoice.SetText(localizedConfirmText.GetSafeLocalizedString()); }
            if (rejectChoice != null) { rejectChoice.SetText(localizedRejectText.GetSafeLocalizedString()); }
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedConfirmText.TableEntryReference,
                localizedRejectText.TableEntryReference,
            };
        }
        #endregion
    }
}
