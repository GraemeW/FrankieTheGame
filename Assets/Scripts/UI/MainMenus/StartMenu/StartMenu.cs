using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using LowDefMustard.UIBox;
using Frankie.Saving;
using Frankie.Utils;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    public sealed class StartMenu : Launcher, ILocalizable
    {
        // Tunables
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedSubHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionStartText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionContinueText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionOptionsText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionQuitText;
        [Header("Prefabs")]
        [SerializeField] private OptionsMenu optionsPrefab;
        [SerializeField] private LoadGameMenu loadGamePrefab;

        // State
        protected override LauncherModel launcherModel { get; } = new();

        #region LauncherMethods
        protected override void BuildChoiceOptions()
        {
            AddLocalizedMenuChoice(localizedOptionStartText, LoadGame);
            AddSeparator(ChoiceSeparatorType.Minor);
            AddLocalizedMenuChoice(localizedOptionContinueText, Continue);
            AddLocalizedMenuChoice(localizedOptionOptionsText, LoadOptions);
            AddSeparator();
            AddLocalizedMenuChoice(localizedOptionQuitText, ExitGame);
        }

        protected override void ResetMenuText()
        {
            launcherModel.titleText = FrankieDebugger.IsDemo() ? localizedSubHeaderText.GetSafeLocalizedString() : string.Empty;
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedSubHeaderText.TableEntryReference,
                localizedOptionStartText.TableEntryReference,
                localizedOptionContinueText.TableEntryReference,
                localizedOptionOptionsText.TableEntryReference,
                localizedOptionQuitText.TableEntryReference
            };
        }
        #endregion

        #region PrivateMethods
        private void LoadGame()
        {
            LoadGameMenu loadGameMenu = Instantiate(loadGamePrefab, startCanvas.transform);
            SetActiveInput(false);
            controller.AddInputReceiver(loadGameMenu, null);
        }

        private void Continue()
        {
            if (string.IsNullOrEmpty(SaveFileManager.GetCurrentSaveName())) { return; } // No-op without a save (keep menu input live)
            SetActiveInput(false);
            SaveFileManager.Continue();
        }

        private void LoadOptions()
        {
            OptionsMenu optionsMenu = Instantiate(optionsPrefab, startCanvas.transform);
            controller.AddInputReceiver(optionsMenu, null);
        }

        private void ExitGame()
        {
            SetActiveInput(false);
            Application.Quit();
        }
        #endregion
    }
}
