using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Utils
{
    public class ShaderPropertySetter : MonoBehaviour
    {
        // Tunables
        [Header("Image Properties")]
        [SerializeField] bool initializeMainTexture = true;
        [SerializeField] bool initializeWorldTexture = false;

        [Header("Linked Assets")]
        [SerializeField] Image image = null;
        [SerializeField] Texture worldTexture = null;

        // Global Shader References
        static string GLOBAL_SHADER_PHASE_REFERENCE = "_Phase";
        static string GLOBAL_WORLD_TEXTURE_REFERENCE = "_WorldTex";
        static string GLOBAL_FADE_TIME_REFERENCE = "_FadeTime";

        private void OnEnable()
        {
            if (image == null || image.material == null) { return; }
            if (initializeMainTexture) { image.material.SetFloat(GLOBAL_SHADER_PHASE_REFERENCE, Time.time); }
            if (initializeWorldTexture) { image.material.SetTexture(GLOBAL_WORLD_TEXTURE_REFERENCE, worldTexture); }
        }

        public Image GetImage() => image;

        public void SetFadeTime(float fadeTime)
        {
            if (image == null || image.material == null) { return; }
            image.material.SetFloat(GLOBAL_FADE_TIME_REFERENCE, fadeTime);
        }
    }
}
