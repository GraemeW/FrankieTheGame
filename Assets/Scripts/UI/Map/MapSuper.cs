using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Rendering;
using Frankie.Utils.Localization;

namespace Frankie.Zones.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class MapSuper : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFlavourTopText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFlavourBottomText;
        [Header("Hookups")]
        [SerializeField][Tooltip("Must match the texture the MapCamera prefab renders into")] private RenderTexture mapRenderTexture;
        [Header("Prefabs")]
        [SerializeField] private MapCamera mapCameraPrefab;

        // State
        private readonly MapModel mapModel = new();
        private MapCamera mapCamera;

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            mapModel.mapTexture = mapRenderTexture;
            GetComponent<UIToolkitMenuView>().SetDataSource(mapModel);
        }

        protected override void StartTriggered()
        {
            mapModel.flavourTopText = localizedFlavourTopText.GetSafeLocalizedString();
            mapModel.flavourBottomText = localizedFlavourBottomText.GetSafeLocalizedString();
        }

        protected override void EnableTriggered()
        {
            if (mapCamera != null) { Destroy(mapCamera.gameObject); }
            if (mapCameraPrefab == null) { return; }

            mapCamera = Instantiate(mapCameraPrefab);
            mapCamera.UpdateMap();
        }

        protected override void DestroyTriggered()
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
