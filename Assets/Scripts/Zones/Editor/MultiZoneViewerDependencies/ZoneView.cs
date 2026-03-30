using UnityEngine;

namespace Frankie.ZoneManagement.UIEditor
{
    public class ZoneView
    {
        public ZoneViewData data;
        public Texture2D texture2D;

        public ZoneView(ZoneViewData zoneViewData, Texture2D texture2D)
        {
            data = zoneViewData;
            this.texture2D = texture2D;
        }
    }
}
