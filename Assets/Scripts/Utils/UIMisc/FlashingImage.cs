using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FlashingImage : MonoBehaviour
    {
        [SerializeField] float frequency = 1f;

        // Cached References
        CanvasGroup canvasGroup = null;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void FixedUpdate()
        {
            canvasGroup.alpha = 0.5f * (1 + Mathf.Sin(2f * Mathf.PI * frequency * Time.time));
        }
    }
}