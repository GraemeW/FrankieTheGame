using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    public sealed class GameWinMenu : Launcher, ILocalizable
    {
        // Tunables
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedGameWinText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionStartMenu;
        [Header("Credits")]
        [SerializeField] private Credits credits;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedCreditsHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedLeadProgrammerText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedGameDesignText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedArtworkText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMusicText;

        // State
        private readonly GameWinMenuModel gameWinMenuModel = new();

        #region LauncherMethods
        protected override LauncherModel launcherModel => gameWinMenuModel;

        protected override void BuildChoiceOptions()
        {
            AddLocalizedMenuChoice(localizedOptionStartMenu, ReloadStartScreen);
        }

        protected override void ResetMenuText()
        {
            gameWinMenuModel.titleText = localizedGameWinText.GetSafeLocalizedString();
            gameWinMenuModel.creditsHeaderText = localizedCreditsHeaderText.GetSafeLocalizedString();
            if (credits != null)
            {
                gameWinMenuModel.creditsLines = credits.GetCreditsEntries()
                    .Select(creditsEntry => new CreditsLine(GetLocalizedRoleTitle(creditsEntry.creditsRole).GetSafeLocalizedString(), creditsEntry.name))
                    .ToList();
            }
        }
        #endregion

        #region PrivateMethods
        private LocalizedString GetLocalizedRoleTitle(CreditsRole creditsRole) => creditsRole switch
        {
            CreditsRole.LeadProgrammer => localizedLeadProgrammerText,
            CreditsRole.GameDesign => localizedGameDesignText,
            CreditsRole.Artwork => localizedArtworkText,
            CreditsRole.Music => localizedMusicText,
            _ => new LocalizedString()
        };
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedGameWinText.TableEntryReference,
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
