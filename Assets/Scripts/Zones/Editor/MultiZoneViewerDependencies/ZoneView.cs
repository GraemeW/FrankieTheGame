using UnityEngine;

namespace Frankie.ZoneManagement.UIEditor
{
    public class ZoneView
    {
        public readonly ZoneViewData data;
        public readonly Texture2D texture2D;
        public Vector2 localImagePosition = Vector2.zero;

        public ZoneView(ZoneViewData zoneViewData, Texture2D texture2D)
        {
            data = zoneViewData;
            this.texture2D = texture2D;
        }

        public void SetLocalImagePosition(Vector2 setLocalImagePosition)
        {
            localImagePosition = setLocalImagePosition;
        }
    }
}
