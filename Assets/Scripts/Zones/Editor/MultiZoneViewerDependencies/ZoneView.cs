using UnityEngine;

namespace Frankie.ZoneManagement.UIEditor
{
    public class ZoneView
    {
        public readonly ZoneViewData data;
        public readonly Texture2D texture2D;
        public Vector2 renderedImageDimensions;
        public Vector2 renderedImageOffset;

        public ZoneView(ZoneViewData zoneViewData, Texture2D texture2D, Vector2 renderedImageDimensions, Vector2 renderedImageOffset)
        {
            data = zoneViewData;
            this.texture2D = texture2D;
            this.renderedImageDimensions = renderedImageDimensions;
            this.renderedImageOffset = renderedImageOffset;
        }
    }
}
