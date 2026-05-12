using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Utils.Localization;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class StartMenu : MonoBehaviour, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedSubHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionStartText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionContinueText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionOptionsText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionQuitText;
        [Header("Hookups")] 
        [SerializeField] private TMP_Text headerField;
        [SerializeField] private TMP_Text subHeaderField;
        [SerializeField] private UIChoiceButton startOptionField;
        [SerializeField] private UIChoiceButton continueOptionField;
        [SerializeField] private UIChoiceButton optionOptionsField;
        [SerializeField] private UIChoiceButton quitOptionField;

        #region UnityMethods

        private void Start()
        {
            if (headerField != null) { headerField.SetText(localizedHeaderText.GetSafeLocalizedString()); }
            if (subHeaderField != null) { subHeaderField.SetText(localizedSubHeaderText.GetSafeLocalizedString()); }
            if (startOptionField != null) { startOptionField.SetText(localizedOptionStartText.GetSafeLocalizedString()); }
            if (continueOptionField != null) { { continueOptionField.SetText(localizedOptionContinueText.GetSafeLocalizedString()); } }
            if (optionOptionsField != null) { optionOptionsField.SetText(localizedOptionOptionsText.GetSafeLocalizedString()); }
            if (quitOptionField != null) { quitOptionField.SetText(localizedOptionQuitText.GetSafeLocalizedString()); }
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedHeaderText.TableEntryReference,
                localizedSubHeaderText.TableEntryReference,
                localizedOptionStartText.TableEntryReference,
                localizedOptionContinueText.TableEntryReference,
                localizedOptionOptionsText.TableEntryReference,
                localizedOptionQuitText.TableEntryReference
            };
        }
        #endregion
    }
}
