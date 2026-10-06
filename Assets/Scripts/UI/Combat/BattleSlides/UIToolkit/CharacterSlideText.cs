using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    [CreateAssetMenu(fileName = "CharacterSlideText", menuName = "UI/Character Slide Text", order = 41)]
    public sealed class CharacterSlideText : ScriptableObject, ILocalizable
    {
        // Localized slide text, shared (as one asset) by every box that hosts character slides
        // Note:  Not linked to ILocalizable auto-update/teardown on rename/delete due to complexity for one-time use here
        
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedHPText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedAPText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedHitMissText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedHitCritText;

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedHPText.TableEntryReference,
                localizedAPText.TableEntryReference,
                localizedHitMissText.TableEntryReference,
                localizedHitCritText.TableEntryReference,
            };
        }
        #endregion

        #region PublicMethods
        public void ApplyTo(CharacterSlideModel model)
        {
            model.hpLabel = localizedHPText.GetSafeLocalizedString();
            model.apLabel = localizedAPText.GetSafeLocalizedString();
            model.hitMissText = localizedHitMissText.GetSafeLocalizedString();
            model.hitCritText = localizedHitCritText.GetSafeLocalizedString();
        }
        #endregion
    }
}
