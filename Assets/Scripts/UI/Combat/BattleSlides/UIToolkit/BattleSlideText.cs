using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    [CreateAssetMenu(fileName = "BattleSlideText", menuName = "UI/Battle Slide Text", order = 41)]
    public sealed class BattleSlideText : ScriptableObject, ILocalizable
    {
        // Localized slide text, shared (as one asset) by everything that hosts character / enemy slides
        // Note:  Not linked to ILocalizable auto-update/teardown on rename/delete due to complexity for one-time use here
        
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedHPText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedAPText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedHitMissText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedHitCritText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFriendFound;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFriendIgnored;

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
                localizedFriendFound.TableEntryReference,
                localizedFriendIgnored.TableEntryReference
            };
        }
        #endregion

        #region PublicMethods
        public void ApplyTo(BattleSlideModel model)
        {
            if (model is CharacterSlideModel characterSlideModel)
            {
                characterSlideModel.hpLabel = localizedHPText.GetSafeLocalizedString();
                characterSlideModel.apLabel = localizedAPText.GetSafeLocalizedString();
            }
            if (model is EnemySlideModel enemySlideModel)
            {
                enemySlideModel.friendFoundText = localizedFriendFound.GetSafeLocalizedString();
                enemySlideModel.friendIgnoredText = localizedFriendIgnored.GetSafeLocalizedString();
            }
            model.hitMissText = localizedHitMissText.GetSafeLocalizedString();
            model.hitCritText = localizedHitCritText.GetSafeLocalizedString();
        }
        #endregion
    }
}
