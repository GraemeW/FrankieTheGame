using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Utils
{
    public class LocalShaderPropertySetter : MonoBehaviour
    {
        // Tunables
        [Header("Image Properties")]
        [SerializeField] bool initializePhase = true;

        [Header("Linked Assets")]
        [SerializeField] Image image = null;

        #region UnityMethods
        private void OnEnable()
        {
            if (image == null || image.material == null) { return; }
            if (initializePhase) { ShaderPropertyRefs.SetShaderPhase(image.material, Time.time); }
        }
        #endregion

        #region PublicMethods
        public Image GetImage() => image;

        public void SetFadeTime(float fadeTime)
        {
            if (image == null || image.material == null) { return; }
            ShaderPropertyRefs.SetFadeTime(image.material, fadeTime);
        }

        public void SetWorldRenderTexture(RenderTexture worldRenderTexture)
        {
            if (image == null || image.material == null || worldRenderTexture == null) { return; }
            ShaderPropertyRefs.SetWorldRenderTexture(image.material, worldRenderTexture);
        }
        #endregion
    }
}
