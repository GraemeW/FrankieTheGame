using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraLink : MonoBehaviour
    {
        // Tunables
        [SerializeField] CanvasSortingOverlayType canvasSortingOverlayType = default;

        // STATIC
        static string SORTING_LAYER_FADER_OVERLAY = "FaderOverlay";
        static string SORTING_LAYER_BATTLE_OVERLAY = "BattleOverlay";

        // Cached References
        Canvas canvas = null;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            if (canvas == null) { GetComponent<Canvas>(); }

            if (canvas != null)
            {
                canvas.worldCamera = Camera.main;
                switch (canvasSortingOverlayType)
                {
                    case CanvasSortingOverlayType.FaderOverlay:
                        canvas.sortingLayerName = SORTING_LAYER_FADER_OVERLAY;
                        break;
                    case CanvasSortingOverlayType.BattleOverlay:
                        canvas.sortingLayerName = SORTING_LAYER_BATTLE_OVERLAY;
                        break;
                    default:
                        break;
                }

            }
        }
    }
}
