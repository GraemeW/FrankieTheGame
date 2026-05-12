using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Utils.Localization;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class GameOverMenu : MonoBehaviour, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedGameOverText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedDefaultName;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionContinue;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionQuit;
        [Header("Hookups")]
        [SerializeField] private TMP_Text gameOverTextField;
        [SerializeField] private TMP_Text nameTextField;
        [SerializeField] private UIChoiceButton continueOptionField;
        [SerializeField] private UIChoiceButton quitOptionField;

        #region UnityMethods

        private void Start()
        {
            if (gameOverTextField != null) { gameOverTextField.SetText(localizedGameOverText.GetSafeLocalizedString()); }
            if (nameTextField != null) { nameTextField.SetText(localizedDefaultName.GetSafeLocalizedString()); }
            if (continueOptionField != null) { continueOptionField.SetText(localizedOptionContinue.GetSafeLocalizedString()); }
            if (quitOptionField != null) { quitOptionField.SetText(localizedOptionQuit.GetSafeLocalizedString()); }
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedGameOverText.TableEntryReference,
                localizedDefaultName.TableEntryReference,
                localizedOptionContinue.TableEntryReference,
                localizedOptionQuit.TableEntryReference,
            };
        }
        #endregion
    }
}
