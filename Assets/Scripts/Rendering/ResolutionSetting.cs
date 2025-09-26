using UnityEngine;

namespace Frankie.Rendering
{
    [System.Serializable]
    public struct ResolutionSetting
    {
        public FullScreenMode fullScreenMode;
        public int width;
        public int height;

        public ResolutionSetting(FullScreenMode fullScreenMode, int width, int height)
        {
            this.fullScreenMode = fullScreenMode;
            this.width = width;
            this.height = height;
        }
    }
}
