using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Utils.Localization;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class GameWinMenu : MonoBehaviour, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedGameWinText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedDefaultName;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionStartMenu;
        [Header("CreditsText")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedCreditsHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedLeadProgrammerText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedGameDesignText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedArtworkText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMusicText;
        
        [Header("Hookups")]
        [SerializeField] private TMP_Text gameOverTextField;
        [SerializeField] private TMP_Text nameTextField;
        [SerializeField] private UIChoiceButton startMenuOptionField;
        [Header("CreditsHookups")]
        [SerializeField] private TMP_Text creditsHeaderField;
        [SerializeField] private CreditsEntry creditsLeadProgrammerField;
        [SerializeField] private CreditsEntry creditsGameDesignField;
        [SerializeField] private CreditsEntry creditsArtworkField;
        [SerializeField] private CreditsEntry creditsMusicField;

        #region UnityMethods

        private void Start()
        {
            // Main Entities
            if (gameOverTextField != null) { gameOverTextField.SetText(localizedGameWinText.GetSafeLocalizedString()); }
            if (nameTextField != null) { nameTextField.SetText(localizedDefaultName.GetSafeLocalizedString()); }
            if (startMenuOptionField != null) { startMenuOptionField.SetText(localizedOptionStartMenu.GetSafeLocalizedString()); }
            // Credits
            if (creditsHeaderField != null) { creditsHeaderField.SetText(localizedCreditsHeaderText.GetSafeLocalizedString()); }
            if (creditsLeadProgrammerField != null) { creditsLeadProgrammerField.SetTitle(localizedLeadProgrammerText.GetSafeLocalizedString()); }
            if (creditsGameDesignField != null) { creditsGameDesignField.SetTitle(localizedGameDesignText.GetSafeLocalizedString()); }
            if (creditsArtworkField != null) { creditsArtworkField.SetTitle(localizedArtworkText.GetSafeLocalizedString()); }
            if (creditsMusicField != null) { creditsMusicField.SetTitle(localizedMusicText.GetSafeLocalizedString()); }
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedGameWinText.TableEntryReference,
                localizedDefaultName.TableEntryReference,
                localizedOptionStartMenu.TableEntryReference,
                localizedCreditsHeaderText.TableEntryReference,
                localizedLeadProgrammerText.TableEntryReference,
                localizedGameDesignText.TableEntryReference,
                localizedArtworkText.TableEntryReference,
                localizedMusicText.TableEntryReference,
            };
        }
        #endregion
    }
}
