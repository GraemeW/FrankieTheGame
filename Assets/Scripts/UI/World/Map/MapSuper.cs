using Frankie.Utils.UI;
using UnityEngine;

namespace Frankie.ZoneManagement.UI
{
    public class MapSuper : UIBox
    {
        // Tunables
        [Header("Map Super Properties")]
        [SerializeField] MapCamera mapCameraPrefab = null;

        // State
        MapCamera mapCamera = null;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (mapCamera != null) { Destroy(mapCamera.gameObject); }
            
            mapCamera = Instantiate(mapCameraPrefab);
            mapCamera.UpdateMap();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (mapCamera != null) { Destroy(mapCamera.gameObject); }
        }
    }
}
