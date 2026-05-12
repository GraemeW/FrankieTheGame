using System;
using System.Collections.Generic;
using Frankie.Utils.Localization;
using UnityEngine;
using TMPro;
using Frankie.Utils.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Frankie.Menu.UI
{
    public class LoadGameEntry : UIChoiceButton, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedLevelLabelText;
        [Header("Hookups")]
        [SerializeField] private TMP_Text indexField;
        [SerializeField] private TMP_Text characterNameField;
        [SerializeField] private TMP_Text levelLabelField;
        [SerializeField] private TMP_Text levelField;

        public void Setup(int index, string characterName, int level, Action action)
        {
            if (action == null) { return; }

            indexField.SetText(index.ToString());
            characterNameField.SetText(characterName);
            levelLabelField.SetText(localizedLevelLabelText.GetSafeLocalizedString());
            levelField.SetText(level.ToString());
            button.onClick.AddListener(action.Invoke);
        }

        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedLevelLabelText.TableEntryReference,
            };
        }
    }
}
