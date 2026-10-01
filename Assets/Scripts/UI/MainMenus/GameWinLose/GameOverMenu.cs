using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using Frankie.Saving;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    public sealed class GameOverMenu : Launcher, ILocalizable
    {
        // Tunables
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedGameOverText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionContinue;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionQuit;

        // State
        protected override LauncherModel launcherModel { get; } = new();
        
        #region LauncherMethods
        protected override void BuildChoiceOptions()
        {
            AddLocalizedMenuChoice(localizedOptionContinue, SaveCorePlayerStateAndContinue);
            AddSeparator();
            AddLocalizedMenuChoice(localizedOptionQuit, ReloadStartScreen);
        }

        protected override void ResetMenuText()
        {
            launcherModel.titleText = localizedGameOverText.GetSafeLocalizedString();
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedGameOverText.TableEntryReference,
                localizedOptionContinue.TableEntryReference,
                localizedOptionQuit.TableEntryReference,
            };
        }
        #endregion

        #region PrivateMethods
        private void SaveCorePlayerStateAndContinue()
        {
            SetActiveInput(false);
            SaveFileManager.SaveCorePlayerStateToSave();
            SaveFileManager.Continue();
        }
        #endregion
    }
}
