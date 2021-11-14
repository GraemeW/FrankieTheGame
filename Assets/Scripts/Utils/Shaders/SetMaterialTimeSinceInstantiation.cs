using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Utils
{
    public class SetMaterialTimeSinceInstantiation : MonoBehaviour
    {
        [SerializeField] Image image = null;
        static string GLOBAL_SHADER_PHASE_REFERENCE = "_Phase";

        private void OnEnable()
        {
            if (image == null || image.material == null) { return; }
            image.material.SetFloat(GLOBAL_SHADER_PHASE_REFERENCE, Time.time);
        }

    }
}
