using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils
{
    public class SetMaterialTimeSinceInstantiation : MonoBehaviour
    {
        [SerializeField] Material material = null;

        static string GLOBAL_SHADER_PHASE_REFERENCE = "_Phase";

        private void OnEnable()
        {
            if (material == null) { return; }

            material.SetFloat(GLOBAL_SHADER_PHASE_REFERENCE, Time.time);
        }

    }
}
