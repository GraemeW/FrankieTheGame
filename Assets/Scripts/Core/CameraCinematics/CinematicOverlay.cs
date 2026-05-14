using System.Collections.Generic;
using Frankie.Utils.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Frankie.Core
{
    [ExecuteInEditMode]
    public class CinematicOverlay : MonoBehaviour, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Speech, true)] private LocalizedString localizedOverlayText;
        
        [Header("Hookups")]
        [SerializeField] private TMP_Text overlayTextField;
        
        #region UnityMethods
        private void Start()
        {
            if (overlayTextField != null) { overlayTextField.SetText(localizedOverlayText.GetLocalizedString()); }
        }

        private void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Speech;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedOverlayText.TableEntryReference,
            };
        }
        #endregion
    }
}
