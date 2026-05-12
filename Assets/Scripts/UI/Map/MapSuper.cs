using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.ZoneManagement.UI
{
    public class MapSuper : UIBox, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFlavourTopText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFlavourBottomText;
        [Header("Hookups")]
        [SerializeField] private TMP_Text flavourTopField;
        [SerializeField] private TMP_Text flavourBottomField;
        [Header("Prefabs")]
        [SerializeField] private MapCamera mapCameraPrefab;

        // State
        private MapCamera mapCamera;

        #region UnityMethods

        private void Start()
        {
            if (flavourTopField != null) { flavourTopField.SetText(localizedFlavourTopText.GetSafeLocalizedString()); }
            if (flavourBottomField != null) { flavourBottomField.SetText(localizedFlavourBottomText.GetSafeLocalizedString()); }
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if (mapCamera != null) { Destroy(mapCamera.gameObject); }
            
            mapCamera = Instantiate(mapCameraPrefab);
            mapCamera.UpdateMap();
        }

        private void OnDestroy()
        {
            if (mapCamera != null) { Destroy(mapCamera.gameObject); }
        }
        #endregion

        #region LocalizationMethods

        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedFlavourTopText.TableEntryReference,
                localizedFlavourBottomText.TableEntryReference,
            };
        }
        #endregion
    }
}
